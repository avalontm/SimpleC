using SimpleC.Types;
using SimpleC.Types.AstNodes;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Parsing
{
    class Parser
    {
        public Token[] Tokens { get; private set; }
        private int readingPosition;
        private Stack<StatementSequenceNode> scopes;
        private Stack<int> bracketCounter;

        public Parser(Token[] tokens)
        {
            this.Tokens = tokens;
            readingPosition = 0;
            scopes = new Stack<StatementSequenceNode>();
            bracketCounter = new Stack<int>();
        }

        public ProgramNode ParseToAst()
        {
            scopes.Push(new ProgramNode());

            while (!eof())
            {
                ProcessNextToken();
            }

            if (scopes.Count != 1)
                throw new ParsingException("Los scopes no están correctamente anidados.");

            return (ProgramNode)scopes.Pop();
        }

        private void ProcessNextToken()
        {
            if (peek() is KeywordToken)
            {
                ProcessKeywordToken();
            }
            else if (peek() is IdentifierToken)
            {
                ProcessIdentifierToken();
            }
            else if (peek() is OpenBraceToken)
            {
                ProcessOpenBrace();
            }
            else if (peek() is CloseBraceToken)
            {
                ProcessCloseBrace();
            }
            else if (peek() is PreprocessorToken)
            {
                ProcessPreprocessor();
            }
            else if (peek() is StringToken)
            {
                ProcessStringToken();
            }
            else
            {
                ProcessOtherTokens();
            }
        }

        private void ProcessKeywordToken()
        {
            var keyword = (KeywordToken)next();
         
            if (!KeywordToken.IsKeyword(keyword.Content))
            {
                throw new ParsingException($"Palabra clave inválida: {keyword.Content} en línea {keyword.Line}, columna {keyword.Column}");
            }

            if (scopes.Count < 1) return;
            if (keyword.IsTypeKeyword)
            {
                VariableType varType = keyword.ToVariableType();
                Token name = readToken<IdentifierToken>();

                if (name != null)
                {
                    ProcessKeyword(varType, name);
                }
                else if(varType is VariableType.Return)
                {
                    ProcessOtherKeywords(keyword);
                }
            }
            else if (keyword.Content == "if" || keyword.Content == "while" || keyword.Content == "switch")
            {
                ProcessControlFlowStatement(keyword);
            }
            else
            {
                ProcessOtherKeywords(keyword);
            }
        }

        private void ProcessIdentifierToken()
        {
            var identifierToken = (IdentifierToken)next();
      
            // Check if it's a custom type
            if (IsCustomType(identifierToken.Content))
            {
                VariableType varType = MapStringToVarType(identifierToken.Content);
                Token name = readToken<IdentifierToken>();
                if (name != null)
                {
                    ProcessKeyword(varType, name);
                }
                return;
            }
          
            if (!KeywordToken.IsKeyword(identifierToken.Content))
            {
          
                if (ParserGlobal.Verify(identifierToken.Content))
                {
                    var node = ParserGlobal.Get(identifierToken.Content);
                    if (node is VariableNode variableNode)
                    {
                        ProcessVariableReference(variableNode, identifierToken);
                    }
                    else if (node is MethodNode methodNode)
                    {
                        ProcessMethodCall(methodNode, identifierToken);
                    }
                    return;
                }

                // Handle undeclared method calls
                if (peek() is OperatorToken && peek().Content == "(")
                {
                    ProcessUndeclaredMethodCall(identifierToken);
                    return;
                }

                throw new ParsingException($"El identificador: {identifierToken.Content} No se encontró.: en línea {identifierToken.Line}, columna {identifierToken.Column}");
            }

            var idNode = new IdentifierNode(identifierToken.Content);
            scopes.Peek().AddStatement(idNode);
        }

        private bool IsCustomType(string content)
        {
            return content == "string" || content == "int" || content == "float" ||
                   content == "char" || content == "bool" || content == "void";
        }

        private VariableType MapStringToVarType(string content)
        {
            switch (content)
            {
                case "string": return VariableType.String;
                case "int": return VariableType.Int;
                case "float": return VariableType.Float;
                case "char": return VariableType.Char;
                case "bool": return VariableType.Bool;
                case "void": return VariableType.Void;
                default: return VariableType.Void;
            }
        }

        private void ProcessStringToken()
        {
            var stringNode = new StringNode(next().Content);
            scopes.Peek().AddStatement(stringNode);

            if (peek() is StatementSperatorToken)
                next();
        }

        private void ProcessPreprocessor()
        {
            Token preprocessorToken = next();
            List<Token> preprocessorTokens = new List<Token> { preprocessorToken };

            if (preprocessorToken.Content == "#" && !eof() && peek().Content == "include")
            {
                preprocessorTokens.Add(next()); // include

                // Read until end of line or semicolon
                while (!eof() && peek().Content != "\n" && peek() is not StatementSperatorToken)
                {
                    var token = next();
                    preprocessorTokens.Add(token);
                }

                var preprocessorNode = new PreprocessorNode(preprocessorTokens);
                scopes.Peek().AddStatement(preprocessorNode);

                if (!eof() && (peek().Content == "\n" || peek() is StatementSperatorToken))
                    next();
            }
        }

        private void ProcessOpenBrace()
        {
            var token = next(); // Consume '{'

            if (bracketCounter.Count == 0)
                bracketCounter.Push(1);
            else
                bracketCounter.Push(bracketCounter.Pop() + 1);

            var blockNode = new BlockNode(token);
            scopes.Peek().AddStatement(blockNode);
            scopes.Push(blockNode);
        }

        private void ProcessCloseBrace()
        {
            next(); // Consume '}'

            if (bracketCounter.Count > 0)
            {
                var count = bracketCounter.Pop();
                count--;

                if (count > 0)
                {
                    bracketCounter.Push(count);
                }
                else
                {
                    // Close current block
                    if (scopes.Count > 1) // Don't close root scope
                    {
                        scopes.Pop();
                    }
                }
            }
            else
            {
                throw new ParsingException("Error de sintaxis: llave de cierre sin apertura correspondiente");
            }
        }

        private void ProcessControlFlowStatement(KeywordToken keyword)
        {
            var statementNode = new ControlFlowNode(keyword.Content);
            scopes.Peek().AddStatement(statementNode);
            scopes.Push(statementNode);

            if (peek().Content == "(")
            {
                List<Token> condition = ReadUntilMatchingParenthesis();
                ((ControlFlowNode)statementNode).SetCondition(condition);
            }
        }

        private List<Token> ReadUntilMatchingParenthesis()
        {
            List<Token> tokens = new List<Token>();
            int parenthesisLevel = 0;
       
            do
            {
                var token = next();
                tokens.Add(token);

                if (token.Content == "(")
                    parenthesisLevel++;
                else if (token.Content == ")")
                    parenthesisLevel--;

            } while (parenthesisLevel > 0 && !eof());
   
            return tokens;
        }

        private void ProcessMethodCall(MethodNode methodNode, Token identifierToken)
        {
            List<Token> arguments = new List<Token>();

            if (peek() is OpenBraceToken)
            {
                arguments = ReadUntilMatchingParenthesis();
            }

            var methodCallNode = new MethodCallNode(methodNode.Type, identifierToken.Content, arguments);
            scopes.Peek().AddStatement(methodCallNode);

            if (peek() is StatementSperatorToken)
                next();
        }

        private void ProcessUndeclaredMethodCall(Token identifierToken)
        {
            Debug.WriteLine($"UndeclaredMethodCall: {identifierToken.Content}");
            List<Token> arguments = new List<Token>();

            if (peek().Content == "(")
            {
                arguments = ReadUntilMatchingParenthesis();
            }

            // Handle known library functions
            VariableType returnType = DetermineReturnType(identifierToken.Content);

            // Register function if not already registered
            if (!ParserGlobal.Verify(identifierToken.Content))
            {
                var methodNode = new MethodNode(returnType, identifierToken.Content, new List<Token>());
                ParserGlobal.Register(identifierToken.Content, methodNode);
            }

            var methodCallNode = new MethodCallNode(returnType, identifierToken.Content, arguments);
            scopes.Peek().AddStatement(methodCallNode);

            if (peek().Content == ";")
                next();
        }

        private VariableType DetermineReturnType(string methodName)
        {
            Debug.WriteLine($"DetermineReturnType: {methodName}");
            switch (methodName)
            {
                case "printf": return VariableType.Void;
                case "scanf": return VariableType.Int;
                default: return VariableType.Void;
            }
        }

        private void ProcessVariableReference(VariableNode variableNode, Token identifierToken)
        {
            Debug.WriteLine($"VariableReference: {identifierToken.Content}");

            // Check if it's actually a method call
            if (peek() is OperatorToken && peek().Content == "(")
            {
                List<Token> arguments = ReadUntilMatchingParenthesis();
                var methodCallNode = new MethodCallNode(variableNode.Type, identifierToken.Content, arguments);
                scopes.Peek().AddStatement(methodCallNode);

                if (peek().Content == ";")
                    next();
                return;
            }

            // Check for assignment
            if (peek() is OperatorToken && peek().Content == "=")
            {
                next(); // Consume '='

                List<Token> valueTokens = new List<Token>();
                while (!eof() && peek().Content != ";")
                {
                    valueTokens.Add(next());
                }

                if (!eof() && peek().Content == ";")
                    next();

                var assignmentNode = new AssignmentNode(identifierToken.Content, valueTokens);
                scopes.Peek().AddStatement(assignmentNode);
            }
            else
            {
                // Simple variable reference
                var varRefNode = new VariableReferenceNode(identifierToken.Content, variableNode.Type);
                scopes.Peek().AddStatement(varRefNode);
            }
        }

        private void ProcessOtherTokens()
        {
            if (peek() is OperatorToken)
            {
                var operatorNode = new OperatorNode(next().Content);
                scopes.Peek().AddStatement(operatorNode);
            }
            else if (peek() is NumberLiteralToken)
            {
                var numberNode = new NumberLiteralNode(int.Parse(next().Content));
                scopes.Peek().AddStatement(numberNode);
            }
            else if (peek() is FloatLiteralToken)
            {
                var floatNode = new FloatLiteralNode(float.Parse(next().Content));
                scopes.Peek().AddStatement(floatNode);
            }
            else if (peek() is CharLiteralToken)
            {
                var charNode = new CharLiteralNode(next().Content.ToCharArray()[0]);
                scopes.Peek().AddStatement(charNode);
            }
            else if (peek() is StatementSperatorToken)
            {
                var separatorNode = new StatementSperatorNode(next().Content);
                scopes.Peek().AddStatement(separatorNode);
            }
            else if (peek() is NewLineToken)
            {
                next();
            }
            else
            {
                // Obtener el tipo de token que causó el error
                var token = peek();
                throw new Exception($"Token desconocido encontrado: {token.GetType().Name}. " +
                             $"Contenido: {token.Content}, Línea: {token.Line}, Columna: {token.Column}");
            }

        }

        private void ProcessOtherKeywords(KeywordToken keyword)
        {
            Debug.WriteLine($"OtherKeywords: {keyword?.Content}");

            if (keyword.ToVariableType() ==  VariableType.Return)
            {
                List<Token> returnTokens = new List<Token> { keyword };
                
                while (!eof() && peek() is not StatementSperatorToken)
                {
                    returnTokens.Add(next());
                }

                if (!eof() && peek() is StatementSperatorToken)
                    returnTokens.Add(next());

                var returnNode = new ReturnNode(returnTokens);
                scopes.Peek().AddStatement(returnNode);
            }
            else
            {
                // Other keywords like break, continue, etc.
                var keywordNode = new KeywordNode(keyword.ToVariableType(), keyword.Content);
                scopes.Peek().AddStatement(keywordNode);

                if (!eof() && peek() is StatementSperatorToken)
                    next();
            }
        }

        void ProcessKeyword(VariableType varType, Token name)
        {
            Debug.WriteLine($"ProcessKeyword: {name.Content}");
            if (name == null)
            {
                throw new ParsingException($"Identificador de nombre esperado para el tipo {varType}");
            }

            // Method declarations
            if (peek() is OpenBraceToken && peek().Content == "(")
            {
                List<Token> parameters = ReadUntilMatchingParenthesis();

                var methodNode = new MethodNode(varType, name.Content, parameters);
                scopes.Peek().AddStatement(methodNode);

                // Use method as new container
                scopes.Push(methodNode);

                // Check for opening block
                if (peek() is OpenBraceToken)
                {
                    ProcessOpenBrace();
                }

                // Check for semicolon (function declaration without body)
                if (peek() is StatementSperatorToken)
                    next();
            }
            // Variable declarations
            else
            {
                List<Token> operatorTokens = new List<Token>(); ;
                List<Token> valueTokens = new List<Token>();

                while (!eof() && peek() is OperatorToken)
                {
                    operatorTokens.Add(next());
                }

                if (operatorTokens.Count > 0)
                {
                    while (!eof() && peek() is not StatementSperatorToken)
                    {
                        valueTokens.Add(next());
                    }
                }

                if (!eof() && peek() is StatementSperatorToken)
                {
                    var token = next();
                }

                // Always set global variables with isRoot=true for correct scope
                bool isGlobalVar = bracketCounter.Count == 0;
                var variableNode = new VariableNode(varType, name, operatorTokens, valueTokens, isGlobalVar);
                scopes.Peek().AddStatement(variableNode);
            }
        }

        private TExpected readToken<TExpected>() where TExpected : Token
        {
            if (peek() is TExpected)
                return (TExpected)next();
            else
                return null;
        }

        [DebuggerStepThrough]
        private Token peek()
        {
            return Tokens[readingPosition];
        }

        [DebuggerStepThrough]
        private Token next()
        {
            var ret = peek();
            readingPosition++;
            return ret;
        }

        [DebuggerStepThrough]
        private bool eof()
        {
            return readingPosition >= Tokens.Length;
        }
    }
}