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
        private int currentLine; // Contador de líneas
        private int currentPosition; // Posición actual en la línea
        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "int", "float", "bool", "void", "return", "char", "string",
            "if", "else", "while", "for", "do", "switch", "case", "default",
            "break", "continue", "goto", "sizeof", "typedef", "struct", "union",
            "enum", "const", "volatile", "static", "extern", "register", "auto",
            "signed", "unsigned", "short", "long", "double"
        };

        public Tokenizer(string code)
        {
            this.Code = code;
            readingPosition = 0;
            currentLine = 1; // Iniciar en la primera línea
            currentPosition = 1; // Iniciar en la posición 1 de la línea
        }

        public Token[] Tokenize()
        {
            var tokens = new List<Token>();
            var builder = new StringBuilder();

            while (!eof())
            {
                skip(CharType.WhiteSpace);
                var _peekType = peekType();

                switch (_peekType)
                {
                    case CharType.Alpha: // Identificadores y palabras clave
                        readToken(builder, CharType.AlphaNumeric);
                        string s = builder.ToString();

                        if (Keywords.Contains(s))
                        {
                            tokens.Add(new KeywordToken(s));
                        }
                        else
                        {
                            tokens.Add(new IdentifierToken(s));
                        }
                        builder.Clear();
                        break;

                    case CharType.Numeric: // Números
                        bool hasDecimal = readNumber(builder);
                        if (hasDecimal)
                        {
                            tokens.Add(new FloatLiteralToken(builder.ToString()));
                        }
                        else
                        {
                            tokens.Add(new NumberLiteralToken(builder.ToString()));
                        }
                        builder.Clear();
                        break;

                    case CharType.Operator: // Operadores
                        readToken(builder, CharType.Operator);
                        tokens.Add(new OperatorToken(builder.ToString()));
                        builder.Clear();
                        break;

                    case CharType.OpenBrace:
                        tokens.Add(new OpenBraceToken(next().ToString()));
                        break;

                    case CharType.CloseBrace:
                        tokens.Add(new CloseBraceToken(next().ToString()));
                        break;

                    case CharType.ArgSeperator:
                        tokens.Add(new ArgSeperatorToken(next().ToString()));
                        break;

                    case CharType.StatementSeperator:
                        tokens.Add(new StatementSperatorToken(next().ToString()));
                        break;

                    case CharType.SingleLineComment:
                        skipSingleLineComment();
                        break;

                    case CharType.MultiLineComment:
                        skipMultiLineComment();
                        break;

                    case CharType.StringDelimiter:
                        tokens.Add(readStringLiteral());
                        break;

                    case CharType.CharDelimiter:
                        tokens.Add(readCharLiteral());
                        break;

                    case CharType.Preprocessor:
                        handlePreprocessor(tokens);
                        break;

                    default:
                        throw new Exception($"El tokenizer encontró un carácter no identificable: '{peek()}' en la línea {currentLine}, posición {currentPosition}");
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
                tokens.Add(new PreprocessorToken("#"));

                string keyword = directive.Substring(1); // Remover el #
                if (!string.IsNullOrEmpty(keyword))
                    tokens.Add(new IdentifierToken(keyword));

                skip(CharType.WhiteSpace);

                // Manejar <stdio.h>
                if (peek() == '<')
                {
                    tokens.Add(new OperatorToken("<"));
                    next(); // Saltar '<'

                    builder.Clear();
                    while (!eof() && peek() != '>')
                    {
                        builder.Append(next());
                    }

                    if (eof())
                        throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentPosition}: #include sin '>' de cierre.");

                    if (peek() != '>')
                        throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentPosition}: Se esperaba un '>' después del nombre de la librería.");

                    tokens.Add(new LibraryToken(builder.ToString())); // Nombre de la libreria o archivo
                    tokens.Add(new OperatorToken(">"));
                    next(); // Saltar '>'
                }
                // Manejar "libreria.h"
                else if (peek() == '"')
                {
                    tokens.Add(new OperatorToken("\""));
                    next(); // Saltar '"'

                    builder.Clear();
                    while (!eof() && peek() != '"')
                    {
                        builder.Append(next());
                    }

                    if (eof())
                        throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentPosition}: #include sin '\"' de cierre.");

                    if (peek() != '"')
                        throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentPosition}: Se esperaba un '\"' después del nombre de la librería.");

                    tokens.Add(new LibraryToken(builder.ToString())); // Nombre de la libreria o archivo
                    tokens.Add(new OperatorToken("\""));
                    next(); // Saltar '"'
                }
                else
                {
                    throw new ParsingException($"Error de sintaxis en la línea {currentLine}, posición {currentPosition}: #include debe tener una apertura de '<' o '\"' para el archivo.");
                }

                return;
            }

            throw new ParsingException($"Directiva de preprocesador desconocida en la línea {currentLine}, posición {currentPosition}: {directive}");
        }


        private void HandleNewLine()
        {
            currentLine++; // Aumenta la cuenta de líneas
            currentPosition = 1; // Reinicia la posición en la línea

            // Si el salto de línea es '\r\n', avanzamos un carácter más
            if (!eof(1) && peek() == '\r' && Code[readingPosition + 1] == '\n')
            {
                readingPosition++; // Saltar el '\n' adicional
            }
        }


        private bool readNumber(StringBuilder builder)
        {
            bool hasDecimalPoint = false;

            while (!eof() && (peekType().HasFlag(CharType.Numeric) || peek() == '.'))
            {
                if (peek() == '.')
                {
                    if (hasDecimalPoint) break; // Si ya había un punto, detener
                    hasDecimalPoint = true;
                }
                builder.Append(next()); // Agregar el carácter al token
                currentPosition++; // Incrementar la posición en la línea
            }

            return hasDecimalPoint;
        }

        private Token readCharLiteral()
        {
            char delimiter = next(); // Consumimos el delimitador de apertura (')
            currentPosition++;

            if (eof()) // Verificamos si el archivo terminó inesperadamente
                throw new Exception($"Fin de archivo inesperado al leer un carácter en la línea {currentLine}, posición {currentPosition}.");

            char charValue = next(); // Leemos el carácter dentro del delimitador
            currentPosition++;

            if (eof() || next() != delimiter) // Verificamos si el siguiente carácter es el delimitador de cierre
                throw new Exception($"Literal de carácter mal formado. Se esperaba un delimitador de cierre en la línea {currentLine}, posición {currentPosition}.");

            return new CharLiteralToken(charValue);
        }

        private Token readStringLiteral()
        {
            var builder = new StringBuilder();

            // Verificar que haya una comilla de apertura
            if (peek() != '"')
                throw new Exception($"Error: Se esperaba una comilla de apertura (\"), pero no se encontró en la línea {currentLine}, posición {currentPosition}.");

            next(); // Saltar la comilla inicial
            currentPosition++; // Incrementar la posición

            while (!eof())
            {
                char currentChar = peek();

                // Si encontramos la comilla de cierre, salimos del bucle
                if (currentChar == '"')
                {
                    next(); // Saltar la comilla de cierre
                    currentPosition++; // Incrementar la posición
                    break;
                }

                // Si encontramos un salto de línea antes de la comilla de cierre, reportamos un error
                if (currentChar == '\n')
                {
                    throw new Exception($"Error: Las cadenas literales no pueden contener saltos de línea en la línea {currentLine}, posición {currentPosition}.");
                }

                builder.Append(next()); // Añadir el carácter a la cadena
                currentPosition++; // Incrementar la posición
            }

            // Si llegamos al final sin encontrar la comilla de cierre, es un error
            if (eof())
                throw new Exception($"Error: Se esperaba una comilla de cierre para la cadena en la línea {currentLine}, posición {currentPosition}.");

            // Retornar el token con el valor de la cadena
            return new StringToken(builder.ToString());
        }


        private void skipSingleLineComment()
        {
            while (!eof() && peek() != '\n')
            {
                next();
                currentPosition++;
            }
        }

        private void skipMultiLineComment()
        {
            while (!eof())
            {
                if (peek() == '*' && next() == '/')
                {
                    next(); // Saltar el '/'
                    break;
                }
                next();
                currentPosition++;
            }
        }

        private void readToken(StringBuilder builder, CharType typeToRead)
        {
            while (!eof() && peekType().HasAnyFlag(typeToRead))
            {
                builder.Append(next());
                currentPosition++; // Incrementar la posición de la línea
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
                case ',':
                    return CharType.ArgSeperator;
                case ';':
                    return CharType.StatementSeperator;
                case '\r':
                case '\n':
                    return CharType.NewLine;
                case '#':
                    return CharType.Preprocessor;
                case '"':
                    return CharType.StringDelimiter;
                case '\'':
                    return CharType.CharDelimiter;
                case '\t':  
                    return CharType.LineSpace; 

            }

            switch (char.GetUnicodeCategory(c))
            {
                case UnicodeCategory.DecimalDigitNumber:
                    return CharType.Numeric;
                case UnicodeCategory.LineSeparator:
                    return CharType.NewLine;
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.UppercaseLetter:
                    return CharType.Alpha;
                case UnicodeCategory.SpaceSeparator:
                    return CharType.LineSpace;
            }

            return CharType.Unknown;
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
