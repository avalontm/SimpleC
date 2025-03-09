using SimpleC.Parsing;
using SimpleC.Types;
using SimpleC.Types.Tokens;
using System.Globalization;
using System.Text;

namespace SimpleC.Lexing
{
    /// <summary>
    /// Tokenizer for the SimpleC language.
    /// </summary>
    class Tokenizer
    {
        public string Code { get; private set; }
        private int readingPosition;
        private int currentLine;
        private int currentColumn;

        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "int", "float", "bool", "void", "return", "char", "string",
            "if", "else", "while", "for", "do", "switch", "case", "default",
            "break", "continue", "short", "long", "double"
        };

        public Tokenizer(string code)
        {
            this.Code = code;
            readingPosition = 0;
            currentLine = 1;
            currentColumn = 1;
        }

        public Token[] Tokenize()
        {
            var tokens = new List<Token>();
            var builder = new StringBuilder();

            while (!eof())
            {
                skip(CharType.WhiteSpace);
                var _peekType = peekType();
                int startColumn = currentColumn;

                switch (_peekType)
                {
                    case CharType.Alpha:
                        readToken(builder, CharType.AlphaNumeric);
                        string s = builder.ToString();
                        tokens.Add(Keywords.Contains(s) ? new KeywordToken(s, currentLine, startColumn) : new IdentifierToken(s, currentLine, startColumn));
                        builder.Clear();
                        break;
                    case CharType.Numeric:
                        bool hasDecimal = readNumber(builder);
                        tokens.Add(hasDecimal ? new FloatLiteralToken(builder.ToString(), currentLine, startColumn) : new NumberLiteralToken(builder.ToString(), currentLine, startColumn));
                        builder.Clear();
                        break;
                    case CharType.Operator:
                        readToken(builder, CharType.Operator);
                        tokens.Add(new OperatorToken(builder.ToString(), currentLine, startColumn));
                        builder.Clear();
                        break;
                    case CharType.OpenBrace:
                        tokens.Add(new OpenBraceToken(next().ToString(), currentLine, startColumn));
                        break;
                    case CharType.CloseBrace:
                        tokens.Add(new CloseBraceToken(next().ToString(), currentLine, startColumn));
                        break;
                    case CharType.ArgSeperator:
                        tokens.Add(new ArgSeperatorToken(next().ToString(), currentLine, startColumn));
                        break;
                    case CharType.StatementSeperator:
                        tokens.Add(new StatementSperatorToken(next().ToString(), currentLine, startColumn));
                        break;
                    case CharType.SingleLineComment:
                        skipSingleLineComment();
                        break;
                    case CharType.MultiLineComment:
                        skipMultiLineComment();
                        break;
                    case CharType.StringDelimiter:
                        tokens.Add(readStringLiteral(startColumn));
                        break;
                    case CharType.CharDelimiter:
                        tokens.Add(readCharLiteral(startColumn));
                        break;
                    case CharType.Preprocessor:
                        handlePreprocessor(tokens);
                        break;

                    case CharType.NewLine: // Aquí se maneja el salto de línea
                        tokens.Add(new NewLineToken("\n", currentLine, startColumn));
                        HandleNewLine(); // Asegura que la línea y columna se actualicen correctamente
                        break;
                    default:
                        throw new Exception($"El tokenizer encontró un carácter no identificable: '{peek()}' en la línea {currentLine}, posición {currentColumn}");
                }
            }

            return tokens.ToArray();
        }

        private void handlePreprocessor(List<Token> tokens)
        {
            var builder = new StringBuilder();
            readToken(builder, CharType.Preprocessor | CharType.AlphaNumeric);
            string directive = builder.ToString();

            if (directive.StartsWith("#"))
            {
                tokens.Add(new PreprocessorToken("#", currentLine, currentColumn));

                string keyword = directive.Substring(1); // Remover el #
                if (!string.IsNullOrEmpty(keyword))
                    tokens.Add(new IdentifierToken(keyword, currentLine, currentColumn));

                skip(CharType.WhiteSpace);

                // Manejar <stdio.h>
                if (peek() == '<')
                {
                    tokens.Add(new OperatorToken("<", currentLine, currentColumn));
                    next(); // Saltar '<'

                    builder.Clear();
                    while (!eof() && peek() != '>')
                    {
                        builder.Append(next());
                    }

                    if (eof())
                        throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentColumn}: #include sin '>' de cierre.");

                    if (peek() != '>')
                        throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentColumn}: Se esperaba un '>' después del nombre de la librería.");

                    tokens.Add(new LibraryToken(builder.ToString(), currentLine, currentColumn)); // Nombre de la libreria o archivo
                    tokens.Add(new OperatorToken(">", currentLine, currentColumn));
                    next(); // Saltar '>'
                }
                // Manejar "libreria.h"
                else if (peek() == '"')
                {
                    tokens.Add(new OperatorToken("\"", currentLine, currentColumn));
                    next(); // Saltar '"'

                    builder.Clear();
                    while (!eof() && peek() != '"')
                    {
                        builder.Append(next());
                    }

                    if (eof())
                        throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentColumn}: #include sin '\"' de cierre.");

                    if (peek() != '"')
                        throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentColumn}: Se esperaba un '\"' después del nombre de la librería.");

                    tokens.Add(new LibraryToken(builder.ToString(), currentLine, currentColumn)); // Nombre de la libreria o archivo
                    tokens.Add(new OperatorToken("\"", currentLine, currentColumn));
                    next(); // Saltar '"'
                }
                else
                {
                    throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentColumn}: #include debe tener una apertura de '<' o '\"' para el archivo.");
                }

                return;
            }

            throw new ParsingException($"Directiva de preprocesador desconocida en la línea {currentLine}, posición {currentColumn}: {directive}");
        }

        private void skipSingleLineComment()
        {
            while (!eof() && peek() != '\n')
            {
                next();
            }
            HandleNewLine();
        }

        private void skipMultiLineComment()
        {
            while (!eof())
            {
                if (peek() == '*' && next() == '/')
                {
                    next();
                    break;
                }
                if (peek() == '\n') HandleNewLine();
                next();
            }
        }

        private bool readNumber(StringBuilder builder)
        {
            bool hasDecimalPoint = false;
            while (!eof() && (peekType().HasFlag(CharType.Numeric) || peek() == '.'))
            {
                if (peek() == '.')
                {
                    if (hasDecimalPoint) break;
                    hasDecimalPoint = true;
                }
                builder.Append(next());
            }
            return hasDecimalPoint;
        }

        private Token readStringLiteral(int startColumn)
        {
            if (peek() != '"')
            {
                throw new Exception($"Error: Se esperaba una comilla de apertura (\") en la línea {currentLine}, posición {startColumn}.");
            }

            next(); // Consumimos la comilla de apertura
            var builder = new StringBuilder();

            while (!eof())
            {
                char currentChar = peek();

                // Si encontramos la comilla de cierre, terminamos la lectura
                if (currentChar == '"')
                {
                    next(); // Consumimos la comilla de cierre
                    return new StringToken(builder.ToString(), currentLine, currentColumn);
                }

                // Si encontramos un salto de línea antes de cerrar la cadena, es un error
                if (currentChar == '\n' || currentChar == '\r')
                {
                    throw new Exception($"Error: Las cadenas no pueden contener saltos de línea sin cierre en la línea {currentLine}, posición {currentColumn}.");
                }

                builder.Append(next()); // Agregar el carácter a la cadena
            }

            // Si terminamos el archivo sin encontrar una comilla de cierre, error
            throw new Exception($"Error: La cadena iniciada en la línea {currentLine}, posición {currentColumn} no tiene una comilla de cierre (\").");
        }


        private Token readCharLiteral(int startColumn)
        {
            next();
            if (eof()) throw new Exception($"Fin de archivo inesperado al leer un carácter en línea {currentLine}, columna {currentColumn}");
            char charValue = next();
            if (eof() || next() != '\'') throw new Exception($"Literal de carácter mal formado en línea {currentLine}, columna {currentColumn}");
            return new CharLiteralToken(charValue.ToString(), currentLine, startColumn);
        }

        private void readToken(StringBuilder builder, CharType typeToRead)
        {
            while (!eof() && peekType().HasAnyFlag(typeToRead))
            {
                builder.Append(next());
            }
        }

        private void skip(CharType typeToSkip)
        {
            while (!eof() && peekType().HasAnyFlag(typeToSkip))
            {
                if (peekType() == CharType.NewLine)
                {
                    HandleNewLine();
                }

                next();
            }
        }

        private void HandleNewLine()
        {
            currentLine++; // Aumenta la cuenta de líneas
            currentColumn = 1; // Reinicia la posición en la línea

            // Si el salto de línea es '\r\n', avanzamos un carácter más
            if (!eof(1) && peek() == '\r' && Code[readingPosition + 1] == '\n')
            {
                readingPosition++; // Saltar el '\n' adicional
            }
            next();
        }

        private CharType peekType() => charTypeOf(peek());

        private CharType charTypeOf(char c)
        {
            if (c == '/')
            {
                if (!eof(1))
                {
                    char nextChar = Code[readingPosition + 1];
                    if (nextChar == '/') return CharType.SingleLineComment;
                    if (nextChar == '*') return CharType.MultiLineComment;
                }
                return CharType.Operator; // Si no es un comentario, es un operador
            }

            switch (c)
            {
                case '+':
                case '-':
                case '*':
                case '%':
                case '&':
                case '|':
                case '=':
                case '<':
                case '>':
                    return CharType.Operator;
                case '(':
                case '[':
                case '{':
                    return CharType.OpenBrace;
                case ')':
                case ']':
                case '}':
                    return CharType.CloseBrace;
                case ';':
                    return CharType.StatementSeperator;
                case ',':
                    return CharType.ArgSeperator;
                case '"':
                    return CharType.StringDelimiter;
                case '\'':
                    return CharType.CharDelimiter;
                case '#':
                    return CharType.Preprocessor;
                case '\n':
                case '\r':
                    return CharType.NewLine;
                case ' ':
                case '\t':
                    return CharType.WhiteSpace;
                default:
                    if (char.IsDigit(c)) return CharType.Numeric;
                    if (char.IsLetter(c) || c == '_') return CharType.Alpha;
                    return CharType.Unknown;
            }
        }

        private char peek() => Code[readingPosition];

        private char next()
        {
            var ret = peek();
            readingPosition++;
            return ret;
        }

        private bool eof() => readingPosition >= Code.Length;
        private bool eof(int lookahead) => readingPosition + lookahead >= Code.Length;
    }
}