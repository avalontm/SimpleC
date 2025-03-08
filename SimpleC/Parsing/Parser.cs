using SimpleC.Types;
using SimpleC.Types.AstNodes;
using SimpleC.Types.Tokens;
using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace SimpleC.Parsing
{
    class Parser
    {
        public Token[] Tokens { get; private set; }

        private int readingPosition;
        private Stack<StatementSequenceNode> scopes;
        private bool isRoot = true;

        public Parser(Token[] tokens)
        {
            this.Tokens = tokens;
            readingPosition = 0;
            scopes = new Stack<StatementSequenceNode>();
        }

        public ProgramNode ParseToAst()
        {
            scopes.Push(new ProgramNode());

            while (!eof())
            {
                if (peek() is KeywordToken)
                {
                    var keyword = (KeywordToken)next();

                    if (!KeywordToken.IsKeyword(keyword.Content))
                    {
                        throw new ParsingException($"Palabra clave inválida: {keyword.Content} en línea {keyword.Line}, columna {keyword.Column}");
                    }

                    if (scopes.Count == 1)
                    {
                        if (keyword.IsTypeKeyword)
                        {
                            VariableType varType = keyword.ToVariableType();
                            Token name = readToken<IdentifierToken>();

                            //vamos a comprobar las palabras reservadas que sean de asignaciones (int, float, char, bool, string)
                            GetKeyword(varType, name);
                        }
                        else
                        {
                            throw new ParsingException($"Se encontró una palabra clave no relacionada con un tipo en el nivel superior: `{keyword.Content}` en línea {keyword.Line}, columna {keyword.Column}");
                        }
                    }
                }
                else if (peek() is IdentifierToken identifierToken)
                {
                    if (!KeywordToken.IsKeyword(identifierToken.Content))
                    {
                        if (ParserGlobal.Verify(identifierToken.Content))
                        {
                            var node = ParserGlobal.Get(identifierToken.Content);
                            if (node is VariableNode variableNode)
                            {
                                GetKeyword(variableNode.Type, identifierToken);
                            }
                            else if (node is MethodNode methodNode)
                            {
                                GetKeyword(methodNode.Type, identifierToken);
                            }

                            continue;
                        }
                        throw new ParsingException($"El identificador: {identifierToken.Content} No se encontró.: en línea {identifierToken.Line}, columna {identifierToken.Column}");
                    }
                    scopes.Push(new IdentifierNode(identifierToken.Content));
                }
                else
                {
                    Debug.WriteLine($"Other: {peek()}");
                    if (peek() is PreprocessorToken preprocessorToken)
                    {
                        var tokens = readUntilChar(new string[] { ">", "\"" });
                        scopes.Push(new PreprocessorNode(tokens));
                    }
                    else if (peek() is OperatorToken operatorToken)
                    {
                        scopes.Push(new OperatorNode(next().Content));
                    }
                    else if (peek() is NumberLiteralToken numberLiteralToken)
                    {
                        scopes.Push(new NumberLiteralNode(int.Parse(next().Content)));
                    }
                    else if (peek() is StatementSperatorToken statementSperatorToken)
                    {
                        scopes.Push(new StatementSperatorNode(next().Content));
                    }
                    else if (peek() is FloatLiteralToken floatLiteralToken)
                    {
                        scopes.Push(new FloatLiteralNode(float.Parse(next().Content)));
                    }
                    else if (peek() is CharLiteralToken charLiteralToken)
                    {
                        scopes.Push(new CharLiteralNode(next().Content.ToCharArray()[0]));
                    }
                    else if (peek() is StringToken stringToken)
                    {
                        scopes.Push(new StringNode(next().Content));
                    }
                    else if (peek() is OpenBraceToken openBraceToken)
                    {
                        scopes.Push(new OpenBraceNode(next().Content));
                    }
                    else if (peek() is CloseBraceToken closeBraceToken)
                    {
                        scopes.Push(new CloseBraceNode(next().Content));
                    }
                }
                scopes.Pop();
            }

            if (scopes.Count != 1)
                throw new ParsingException("Los scopes no están correctamente anidados.");

            return (ProgramNode)scopes.Pop();
        }

        void GetKeyword(VariableType varType, Token name)
        {
            //Console.WriteLine($"GetKeyword: {varType} | {name} | {isRoot}");
            switch (varType)
            {
                case VariableType.Int:
                case VariableType.Float:
                case VariableType.Char:
                case VariableType.Bool:
                case VariableType.String:

                    Token _operator = next();

                    if (_operator.Content == "(")
                    {
                        List<Token> _parameter = new List<Token>();
                        _parameter.Add(_operator);
                        while (next().Content != ")")
                        {
                            _parameter.Add(peek());
                        }

                        var openKey = next();

                        if (scopes.Count == 1)
                        {
                            isRoot = false;
                            // Estamos en el nivel raíz
                            var func = new MethodNode(varType, name.Content, _parameter);
                            scopes.Peek().AddStatement(func);
                            scopes.Push(func);
                        }
                        else
                        {
                            isRoot = false;
                            // Estamos dentro de un ámbito
                            var func = new MethodNode(varType, name.Content, _parameter);
                            scopes.Push(func);
                        }

                        return;
                    }

                    List<Token> _parameters = new List<Token>();
                    _parameters.Add(peek());
                    while (next().Content != ";")
                    {
                        _parameters.Add(peek());
                    }

                    scopes.Push(new VariableNode(varType, name, _operator, _parameters, isRoot));

                    break;
                case VariableType.Void:
                    List<Token> tokens = new List<Token>();

                    tokens.Add(peek());

                    while (next().Content != ")")
                    {
                        tokens.Add(peek());
                    }

                    if (peek().Content == ";")
                    {
                        tokens.Add(peek());
                    }
                    next();
                    isRoot = true;
                    scopes.Push(new MethodNode(VariableType.Void, name.Content, tokens));
                    isRoot = false;
                    break;
                case VariableType.Printf:

                    tokens = new List<Token>();

                    tokens.Add(peek());

                    while (next().Content != ")")
                    {
                        tokens.Add(peek());
                    }

                    if (peek().Content == ";")
                    {
                        tokens.Add(peek());
                    }
                    next();

                    scopes.Push(new MethodNode(VariableType.Printf, "printf", tokens));
                    break;
                case VariableType.Return:

                    tokens = new List<Token>();
                    tokens.Add(name);
                    tokens.Add(peek());

                    while (next().Content != ";")
                    {
                        tokens.Add(peek());
                    }
                    while (next().Content != "}") { }
                
                    scopes.Push(new ReturnNode(tokens));
                    isRoot = true;
                    break;
                default:
                    scopes.Push(new KeywordNode(varType, name.Content));

                    break;
            }
        }

        private TExpected readToken<TExpected>() where TExpected : Token
        {
            if (peek() is TExpected)
                return (TExpected)next();
            else
                return null;
        }

        private IEnumerable<Token> readUntilChar(string[] chars)
        {
            int quoteCount = 0;  // Contador de comillas dobles (") encontradas

            // Lee hasta encontrar un token cuyo contenido esté en el array de caracteres
            while (!eof())
            {
                var token = next(); // Obtiene el siguiente token
                yield return token; // Devuelve el token actual

                // Si encontramos un " o cualquier otro carácter en el array
                if (chars.Contains(token.Content))
                {
                    // Si es un " (comillas dobles), aumentamos el contador
                    if (token.Content == "\"")
                    {
                        quoteCount++; // Incrementa el contador solo si es una comilla doble

                        // Si es el segundo " encontrado, terminamos el proceso
                        if (quoteCount == 2)
                        {
                            yield break; // Detiene la ejecución una vez encontramos el segundo "
                        }
                    }
                    else
                    {
                        // Para otros caracteres como >, se detiene en el primer encuentro
                        yield break;
                    }
                }
            }
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
