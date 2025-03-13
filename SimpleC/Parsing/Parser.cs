using SimpleC.Types;
using SimpleC.Types.AstNodes;
using SimpleC.Types.Tokens;
using SimpleC.Utils;
using System.Diagnostics;

namespace SimpleC.Parsing
{
    class Parser
    {
        #region FUNCIONES INDISPENSABLES

        public Token[] Tokens { get; private set; }
        private int readingPosition;
        private Stack<StatementSequenceNode> scopes;
        private Stack<int> bracketCounter;

        public int BracketCounter { get { return bracketCounter.Count; } }

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

        public StatementSequenceNode? Peek
        {
            get { return scopes.Count > 0 ? scopes.Peek() : null; }
        }
        #endregion

        public static Parser? Instance { get; private set; }
        public Parser(Token[] tokens)
        {
            Instance = this;
            this.Tokens = tokens;
            readingPosition = 0;
            scopes = new Stack<StatementSequenceNode>();
            bracketCounter = new Stack<int>();
        }

        public ProgramNode ParseToAst()
        {
            scopes.Push(new ProgramNode());
            try
            {
                while (!eof())
                {
                    ProcessNextToken();
                }

                // Antes de verificar el conteo, asegúrate de que todos los alcances estén correctamente balanceados
                while (scopes.Count > 1)
                {
                    var scope = scopes.Pop();
                    if (scope is BlockNode blockNode && !blockNode.IsComplete())
                    {
                        // Obtener la ubicación de la llave de apertura para el bloque incompleto
                        Token openBrace = blockNode.OpenBraceToken;
                        throw new ParsingException(
                            $"Error de sintaxis en línea {openBrace.Line}, columna {openBrace.Column}: " +
                            "bloque abierto pero nunca cerrado al final del archivo.");
                    }
                }
                return (ProgramNode)scopes.Pop();
            }
            catch (Exception ex)
            {
#if DEBUG
                ColorParser.WriteLine($"[color=red]{ex}[/color]");
#else
                ColorParser.WriteLine($"[color=red]{ex.Message}[/color]");
#endif
                return null;
            }
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
                else if (varType is VariableType.Return)
                {
                    ProcessOtherKeywords(keyword);
                }
            }
            else if (KeywordToken.IsKeyword(keyword.Content))
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

            if (!KeywordToken.IsKeyword(identifierToken.Content))
            {
                // Verificar si el identificador existe en alguno de los ámbitos (local o global)
                if (scopes.Peek().Verify(identifierToken.Content) || ParserGlobal.Verify(identifierToken.Content))
                {
                    // Intentar obtener el nodo desde el ámbito local primero
                    bool islocal = scopes.Peek().Verify(identifierToken.Content);
                    StatementSequenceNode? node = null;

                    // Si no se encuentra en el ámbito local, intentar obtenerlo del ámbito global
                    if (!islocal)
                    {
                        node = ParserGlobal.Get(identifierToken.Content);
                    }

                    if (islocal)
                    {
                        var vartype = scopes.Peek().Get(identifierToken.Content);
                        ProcessVariableReference(new VariableNode(vartype.Value, identifierToken, new List<Token>(), new List<Token>()), identifierToken);
                        return;
                    }
                    // Procesar el nodo según su tipo
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

                if (IsCustom(identifierToken.Content))
                {
                    // apra las funciones integradas (personalizadeas)
                    var methodCallNode = new MethodCallNode(DetermineReturnType(identifierToken.Content), identifierToken.Content, GetTokens());
                    scopes.Peek().AddStatement(methodCallNode);
                    return;
                }

                // Manejar llamadas a métodos no declarados
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

        private bool IsCustom(string methodName)
        {
            switch (methodName)
            {
                case "printf": return true;
                case "scanf": return true;
                default: return false;
            }
        }
        private List<Token> GetProcessorTokens()
        {
            List<Token> tokens = new List<Token>();

            // Procesar los tokens hasta que encontremos el cierre esperado (">" o "\"") o un salto de línea
            while (!(peek() is StatementSperatorToken && (peek().Content == ">" || peek().Content == "\"")) && !(peek() is NewLineToken))
            {
                var token = next();
                tokens.Add(token);
            }

            // Si el siguiente token es un delimitador de cierre, lo agregamos a la lista de tokens
            if (peek() is StatementSperatorToken && (peek().Content == ">" || peek().Content == "\""))
            {
                tokens.Add(next());
            }

            // Si encontramos un salto de línea, lo agregamos también
            if (peek() is NewLineToken)
            {
                tokens.Add(next());
            }

            return tokens;
        }


        private List<Token> GetTokens()
        {
            List<Token> tokens = new List<Token>();

            while (!(peek() is StatementSperatorToken && peek().Content == ";") && peek() is not NewLineToken)
            {
                var token = next();
                tokens.Add(token);
            }

            if (peek() is StatementSperatorToken && peek().Content == ";")
            {
                tokens.Add(peek());
            }

            return tokens;
        }

        private void ProcessStringToken()
        {
            var stringNode = new StringNode(GetTokens());
            scopes.Peek().AddStatement(stringNode);
        }

        private void ProcessPreprocessor()
        {
            // Crear y agregar el nodo de preprocesador
            var preprocessorNode = new PreprocessorNode(GetProcessorTokens());
            scopes.Peek().AddStatement(preprocessorNode);
        }

        private void ProcessOpenBrace()
        {
            var token = next(); // Consume '{'
            List<ParameterNode> Parameters = new List<ParameterNode>();

            BlockNode blockNode = new BlockNode();
            blockNode.OpenBraceToken = token; // Guardar el token de apertura

            // Determinar si este bloque pertenece a un nodo de control de flujo
            bool isControlFlowBlock = scopes.Peek() is ControlFlowNode;

            if (scopes.Peek() is MethodNode)
            {
                foreach (var node in scopes.Peek().SubNodes)
                {
                    if (node is ParameterNode parameterNode)
                    {
                        Parameters.Add(parameterNode);
                    }
                }
            }

            // Agregar bloque al alcance actual
            scopes.Peek().AddStatement(blockNode);

            if (scopes.Peek() is MethodNode methodNode)
            {
                blockNode.SetParameters(methodNode, Parameters);
            }

            // Empujar el nuevo bloque al stack de scopes
            scopes.Push(blockNode);

            // Si es un bloque de control de flujo, marcar de alguna manera
            if (isControlFlowBlock)
            {
                blockNode.IsControlFlowBlock = true; // Necesitarás agregar esta propiedad a BlockNode
            }

            // Asegurar que el contador de llaves está sincronizado
            if (bracketCounter.Count == 0)
            {
                bracketCounter.Push(1);
            }
            else
            {
                bracketCounter.Push(bracketCounter.Pop() + 1);
            }
        }

        // Modificaciones al método ProcessCloseBrace para manejar correctamente el cierre de bloques
        private void ProcessCloseBrace()
        {
            var token = next(); // Consume '}'

            if (bracketCounter.Count == 0)
            {
                throw new ParsingException($"Error de sintaxis en línea {token.Line}, columna {token.Column}: llave de cierre sin apertura correspondiente");
            }

            // Decrementar el contador de llaves
            var count = bracketCounter.Pop();
            count--;

            if (count > 0)
            {
                bracketCounter.Push(count);
            }

            // Cerrar el alcance actual
            if (scopes.Count > 1)
            {
                // Obtener el bloque actual antes de sacarlo de la pila
                var currentBlock = scopes.Peek();

                // Guardar el token de cierre en el bloque actual
                if (currentBlock is BlockNode blockNode)
                {
                    blockNode.CloseBraceToken = token; // Guardar el token de cierre
                    blockNode.HasReturn();

                    // Verificar si este bloque pertenece a una estructura de control de flujo
                    bool isControlFlowBlock = blockNode.IsControlFlowBlock;

                    // Quitar el bloque actual de la pila
                    scopes.Pop();

                    // Si el bloque era parte de un control de flujo, también sacamos ese nodo
                    if (isControlFlowBlock && scopes.Count > 0 && scopes.Peek() is ControlFlowNode)
                    {
                        scopes.Pop();
                    }
                }
                else
                {
                    scopes.Pop();
                }

                // Si el nuevo scope en la pila es un `MethodNode` y `count == 0`, cerrar el método
                if (count == 0 && scopes.Count > 0 && scopes.Peek() is MethodNode)
                {
                    Debug.WriteLine($"Finalizando método en línea {token.Line}, columna {token.Column}");
                    scopes.Pop();
                }
            }
            else
            {
                throw new ParsingException($"Error de sintaxis en línea {token.Line}, columna {token.Column}: intento de cerrar un alcance no abierto.");
            }
        }

        private void ProcessControlFlowStatement(KeywordToken keyword)
        {
            List<Token> condition = new List<Token>();
            ControlFlowNode statementNode = new ControlFlowNode(keyword.Content);
            scopes.Peek().AddStatement(statementNode);
            scopes.Push(statementNode);


            if (peek() is OpenBraceToken && !eof() && peek().Content == "(")
            {
                condition = ReadUntilMatchingParenthesis();

            }
            else if (keyword.Content == "case")
            {
                condition = ReadUntilCaseColon();
            }
            else if (keyword.Content == "default")
            {
                condition = ReadUntilCaseColon();
            }

            statementNode.SetCondition(condition);
        }

        private List<Token> ReadUntilCaseColon()
        {
            List<Token> tokens = new List<Token>();
            var token = peek();  // Obtener el siguiente token

            do
            {
                token = next();  // Obtener el siguiente token
                tokens.Add(token);   // Agregarlo a la lista de tokens

                // Continuar hasta encontrar el ':' o un salto de línea
            } while (!(token is StatementSperatorToken && token.Content == ":") && !(eof() && token is not NewLineToken));

            // Verificar que haya encontrado ':' antes del salto de línea
            if (!(token is StatementSperatorToken && token.Content == ":"))
            {
                string errorMessage = $"Se esperaba ':' antes de un salto de línea en la línea {token.Line}, columna {token.Column}.";
                throw new InvalidOperationException(errorMessage);
            }

            return tokens;
        }

        private List<Token> ReadUntilMatchingParenthesis()
        {
            List<Token> tokens = new List<Token>();
            int parenthesisLevel = 0;

            do
            {
                var token = next();
                tokens.Add(token);

                if (token is NewLineToken)
                {
                    throw new Exception($"No se encontró un ')' antes de un salto de línea. (Línea: {token.Line}, Columna: {token.Column})");
                }

                if (token.Content == "(")
                    parenthesisLevel++;
                else if (token.Content == ")")
                    parenthesisLevel--;

            } while (parenthesisLevel > 0 && !eof());

            if (parenthesisLevel > 0)
            {
                throw new Exception($"Falta un ')' para cerrar el paréntesis. (Línea: {tokens.Last().Line}, Columna: {tokens.Last().Column})");
            }

            return tokens;
        }


        private void ProcessMethodCall(MethodNode methodNode, Token identifierToken)
        {
            Debug.WriteLine($"ProcessMethodCall: {methodNode}");
            List<Token> arguments = new List<Token>();

            if (peek() is OpenBraceToken)
            {
                arguments = ReadUntilMatchingParenthesis();
            }

            // Filtro para ignorar los NewLineTokens
            while (peek() is not StatementSperatorToken && peek() is not NewLineToken)
            {
                next();
            }

            if (peek() is StatementSperatorToken && peek().Content == ";")
            {
                arguments.Add(next());
            }

            // Crear el nodo de llamada al método
            var methodCallNode = new MethodCallNode(methodNode.Type, identifierToken.Content, arguments);
            scopes.Peek().AddStatement(methodCallNode);
        }


        private void ProcessUndeclaredMethodCall(Token identifierToken)
        {
            Debug.WriteLine($"ProcessUndeclaredMethodCall: {identifierToken.Content}");
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
                MethodNode methodNode = new MethodNode(returnType, identifierToken.Content, new List<Token>());
                ParserGlobal.Register(identifierToken.Content, methodNode);
            }

            if (peek().Content == ";")
                arguments.Add(next());

            var methodCallNode = new MethodCallNode(returnType, identifierToken.Content, arguments);
            scopes.Peek().AddStatement(methodCallNode);
        }

        private VariableType DetermineReturnType(string methodName)
        {
            switch (methodName)
            {
                case "printf": return VariableType.Void;
                case "scanf": return VariableType.Int;
                default: return VariableType.Void;
            }
        }

        private void ProcessVariableReference(VariableNode variableNode, Token identifierToken)
        {
            Debug.WriteLine($"ProcessVariableReference: {variableNode}");
            // Verificar si es una llamada a método (ejemplo: variable())
            if (peek() is OperatorToken && peek().Content == "(")
            {
                List<Token> arguments = ReadUntilMatchingParenthesis();
                var methodCallNode = new MethodCallNode(variableNode.Type, identifierToken.Content, arguments);
                scopes.Peek().AddStatement(methodCallNode);

                // Si hay un punto y coma al final, consumirlo
                if (!eof() && peek().Content == ";")
                    next();
                return;
            }

            // Lista para almacenar todos los operadores encontrados
            List<Token> operatorTokens = new List<Token>();

            // Si se encuentra un operador, agregar todos los operadores consecutivos a la lista
            while (!eof() && peek() is OperatorToken)
            {
                operatorTokens.Add(next()); // Consumir y agregar a la lista
            }

            // Verificar si después de los operadores hay una asignación de valor
            if (operatorTokens.Count > 0)
            {
                List<Token> valueTokens = new List<Token>();

                // Recoger los valores hasta encontrar un punto y coma
                while (!eof() && peek().Content != ";")
                {
                    valueTokens.Add(next());
                }

                // Si hay un punto y coma al final, consumirlo
                if (!eof() && peek().Content == ";")
                    next();

                // Crear un nodo de asignación con la variable, operadores y valores
                var assignmentNode = new AssignmentNode(identifierToken, operatorTokens, valueTokens);
                scopes.Peek().AddStatement(assignmentNode);
            }
            else
            {
                // Si no hay operadores, es una referencia simple a la variable
                var varRefNode = new VariableReferenceNode(identifierToken, variableNode.Type);
                scopes.Peek().AddStatement(varRefNode);
            }
        }


        private void ProcessOtherTokens()
        {
            if (peek() is OperatorToken)
            {
                var operatorNode = new OperatorNode(next());
                scopes.Peek().AddStatement(operatorNode);
            }
            else if (peek() is NumberLiteralToken)
            {
                var numberNode = new NumberLiteralNode(GetTokens());
                scopes.Peek().AddStatement(numberNode);
            }
            else if (peek() is FloatLiteralToken)
            {
                var floatNode = new FloatLiteralNode(next());
                scopes.Peek().AddStatement(floatNode);
            }
            else if (peek() is CharLiteralToken)
            {
                var charNode = new CharLiteralNode(next().Content.ToCharArray()[0]);
                scopes.Peek().AddStatement(charNode);
            }
            else if (peek() is StatementSperatorToken)
            {
                var separatorNode = new StatementSperatorNode(next());
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

            if (keyword.ToVariableType() == VariableType.Return)
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
                returnNode.SetOwner(scopes.Peek());
            }
            else
            {
                var keywordNode = new KeywordNode(keyword.ToVariableType(), keyword.Content);
                scopes.Peek().AddStatement(keywordNode);

                if (!eof() && peek() is StatementSperatorToken)
                    next();
            }
        }

        void ProcessKeyword(VariableType varType, Token name)
        {

            if (name == null)
            {
                throw new ParsingException($"Identificador de nombre esperado para el tipo {varType}");
            }

            // Method declarations
            if (peek() is OpenBraceToken && peek().Content == "(")
            {
                List<Token> parameters = ReadUntilMatchingParenthesis();

                // Create method node
                var methodNode = new MethodNode(varType, name.Content, parameters);

                // Add method to current scope
                scopes.Peek().AddStatement(methodNode);

                // Push method as new scope
                scopes.Push(methodNode);

                // Check for opening block - method body
                if (peek() is OpenBraceToken)
                {
                    ProcessOpenBrace();
                }
                else if (peek() is StatementSperatorToken)
                {
                    // Function declaration without body (e.g., in header files)
                    next();
                    // Pop method scope since there's no body
                    scopes.Pop();
                }
            }
            // Variable declarations
            else
            {
                List<Token> operatorTokens = new List<Token>();
                List<Token> valueTokens = new List<Token>();

                // Ignorar los NewLineToken mientras que el próximo token sea un operador
                while (!eof() && peek() is OperatorToken && peek() is not NewLineToken)
                {
                    operatorTokens.Add(next());
                }


                // Determinar si la variable es global (según el contador de paréntesis)
                bool isGlobalVar = bracketCounter.Count == 0;

                // Crear un nodo de variable
                var variableNode = new VariableNode(varType, name, operatorTokens, GetTokens());

                // Agregar el nodo al alcance actual
                scopes.Peek().AddStatement(variableNode);

            }
        }

    }
}