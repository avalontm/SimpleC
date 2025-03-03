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

        public Tokenizer(string code)
        {
            this.Code = code;
            readingPosition = 0;
        }

        public Token[] Tokenize()
        {
            var tokens = new List<Token>();
            var builder = new StringBuilder();

            while (!eof())
            {
                skip(CharType.WhiteSpace);

                switch (peekType())
                {
                    case CharType.Alpha: // Identificadores y palabras clave
                        readToken(builder, CharType.AlphaNumeric);
                        string s = builder.ToString();
                        tokens.Add(KeywordToken.IsKeyword(s) ? new KeywordToken(s) : new IdentifierToken(s));
                        builder.Clear();
                        break;

                    case CharType.Numeric: // Números
                        bool hasDecimal = readNumber(builder);
                        if (hasDecimal)
                        {
                            tokens.Add(new DecimalLiteralToken(builder.ToString()));
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

                    case CharType.Preprocessor:
                        handlePreprocessor(tokens);
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

                    default:
                        throw new Exception($"El tokenizer encontró un carácter no identificable: '{peek()}'");
                }
            }

            return tokens.ToArray();
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
            }

            return hasDecimalPoint;
        }

        private Token readCharLiteral()
        {
            char delimiter = next(); // Consumimos el delimitador de apertura (')

            if (eof()) // Verificamos si el archivo terminó inesperadamente
                throw new Exception("Fin de archivo inesperado al leer un carácter.");

            char charValue = next(); // Leemos el carácter dentro del delimitador

            if (eof() || next() != delimiter) // Verificamos si el siguiente carácter es el delimitador de cierre
                throw new Exception("Literal de carácter mal formado. Se esperaba un delimitador de cierre.");

            // Identificamos si el char es normal, un puntero o un array
            int? size = null; // Para arreglos con tamaño opcional
            bool isPointer = false;

            while (!eof())
            {
                char nextChar = peek(); // Miramos el siguiente carácter sin consumirlo

                if (nextChar == '*') // Puntero a carácter (char*)
                {
                    next(); // Consumimos '*'
                    isPointer = true;
                }
                else if (nextChar == '[') // Posible arreglo de caracteres (char[] o char[N])
                {
                    next(); // Consumimos '['

                    StringBuilder sizeBuilder = new StringBuilder();
                    while (!eof() && char.IsDigit(peek())) // Leer el número dentro de los corchetes (si lo hay)
                    {
                        sizeBuilder.Append(next());
                    }

                    if (peek() == ']') // Confirmamos que el cierre de corchetes es correcto
                    {
                        next(); // Consumimos ']'

                        if (sizeBuilder.Length > 0)
                        {
                            size = int.Parse(sizeBuilder.ToString()); // Si hay número, lo convertimos
                        }
                    }
                    else
                    {
                        throw new Exception("Literal de carácter mal formado. Se esperaba ']' al final.");
                    }
                }
                else
                {
                    break;
                }
            }

            // Retornamos el token correcto según el tipo detectado
            if (isPointer)
            {
                return new CharPointerToken(charValue);
            }
            else if (size.HasValue || peek() == ']') // Si tiene tamaño o simplemente `char[]`
            {
                return new CharArrayToken(charValue, size);
            }

            return new CharLiteralToken(charValue);
        }



        private Token readStringLiteral()
        {
            var builder = new StringBuilder();
            next(); // Saltar la comilla inicial

            while (!eof() && peek() != '"')
            {
                builder.Append(next());
            }

            if (eof()) throw new Exception("Error: Se esperaba una comilla de cierre para la cadena.");

            next(); // Saltar la comilla de cierre
            return new StringToken(builder.ToString());
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

                if (peek() == '<') // Manejar <stdio.h>
                {
                    tokens.Add(new OperatorToken("<"));
                    next(); // Saltar '<'

                    builder.Clear();
                    while (!eof() && peek() != '>')
                        builder.Append(next());

                    if (eof())
                        throw new Exception("Error de sintaxis: #include sin '>' de cierre.");

                    tokens.Add(new LibraryToken(builder.ToString())); // Nombre de la libreria o archivo
                    tokens.Add(new OperatorToken(">"));
                    next(); // Saltar '>'
                }
                else if (peek() == '"') // Manejar "myheader.h"
                {
                    tokens.Add(new OperatorToken("\""));
                    next(); // Saltar '"'

                    builder.Clear();
                    while (!eof() && peek() != '"')
                        builder.Append(next());

                    if (eof())
                        throw new Exception("Error de sintaxis: #include sin '\"' de cierre.");

                    tokens.Add(new LibraryToken(builder.ToString())); // Nombre de la libreria o archivo
                    tokens.Add(new OperatorToken("\""));
                    next(); // Saltar '"'
                }

                return;
            }

            throw new Exception($"Directiva de preprocesador desconocida: {directive}");
        }

        private void skipSingleLineComment()
        {
            while (!eof() && peek() != '\n')
                next();
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
            }
        }

        private void readToken(StringBuilder builder, CharType typeToRead)
        {
            while (!eof() && peekType().HasAnyFlag(typeToRead))
                builder.Append(next());
        }

        private void skip(CharType typeToSkip)
        {
            while (peekType().HasAnyFlag(typeToSkip))
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
