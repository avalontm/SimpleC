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

                    if (scopes.Count == 1) // Estamos en el nivel superior, las únicas palabras clave válidas son los tipos de variables, el inicio de una variable o la definición de una función.

                    {
                        if (keyword.IsTypeKeyword)
                        {
                            var varType = keyword.ToVariableType();
                            var name = readToken<IdentifierToken>(); //Debe estar seguido por un identificador.

                            scopes.Push(new KeywordNode(varType, name.Content));

                            scopes.Pop();
                        }
                        else
                        {
                            throw new ParsingException($"Se encontró una palabra clave no relacionada con un tipo en el nivel superior., `{peek().Content}`");
                        }
                    }

                }
                else // Top 0 (Global)
                {
                    Debug.WriteLine($"Peek: {peek().GetType()}");
                    if (peek() is PreprocessorToken preprocessorToken)
                    {
                        var tokens = readUntilChar(new string[] { ">", "\"" });
                        scopes.Push(new PreprocessorNode(tokens));
                    }
                    else if (peek() is IdentifierToken identifierToken)
                    {
                        scopes.Push(new IdentifierNode(next().Content));
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

                    scopes.Pop();
                }
            }

            if (scopes.Count != 1)
                throw new ParsingException("Los scopes no están correctamente anidados.");

            return (ProgramNode)scopes.Pop();
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

        private IEnumerable<Token> readTokenSeqence(params Type[] expectedTypes)
        {
            foreach (var t in expectedTypes)
            {
                if (!t.IsAssignableFrom(peek().GetType()))
                    throw new ParsingException("Unexpected token");
                yield return next();
            }
        }

        private IEnumerable<Token> readUntilClosingBrace()
        {
            //TODO: Only allow round braces, handle nested braces!
            while (!eof() && !(peek() is CloseBraceToken))
                yield return next();
            next(); //skip the closing brace
        }

        private IEnumerable<Token> readUntilStatementSeperator()
        {
            while (!eof() && !(peek() is StatementSperatorToken))
                yield return next();
            next(); //skip the semicolon
        }

        private TExpected readToken<TExpected>() where TExpected : Token
        {
            if (peek() is TExpected)
                return (TExpected)next();
            else
                throw new ParsingException("Unexpected token " + peek());
        }

        [DebuggerStepThrough]
        private Token peek()
        {
            //TODO: Check for eof()
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
