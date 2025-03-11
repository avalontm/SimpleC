using SimpleC.Parsing;

namespace SimpleC.Types.Tokens
{
    class KeywordToken : Token
    {
        private static readonly Dictionary<string, KeywordType> validKeywords = new Dictionary<string, KeywordType>()
        {
            { "int", KeywordType.Int },
            { "float", KeywordType.Float },
            { "bool", KeywordType.Bool },
            { "return", KeywordType.Return },
            { "void", KeywordType.Void },
            { "include", KeywordType.Include },
            { "string", KeywordType.String },
            { "char", KeywordType.Char },
            { "true", KeywordType.True },
            { "false", KeywordType.False },
            { "default", KeywordType.Default },

            { "if", KeywordType.If },
            { "else", KeywordType.Else },
            { "for", KeywordType.For },
            { "switch", KeywordType.Switch },
            { "case", KeywordType.Case },
            { "break", KeywordType.Break },
            { "while", KeywordType.While },
            { "do", KeywordType.Do },
        };

        // Diccionario con traducciones de las palabras clave
        private static readonly Dictionary<string, string> validKeywordsTranslations = new Dictionary<string, string>()
        {
            { "int", "entero" },
            { "float", "flotante" },
            { "bool", "booleano" },
            { "return", "retornar" },
            { "void", "vacío" },
            { "include", "incluir" },
            { "string", "cadena" },
            { "char", "carácter" },
            { "true", "verdadero" },
            { "false", "falso" },
            { "default", "predeterminado" },

            { "if", "si" },
            { "else", "sino" },
            { "for", "para" },
            { "switch", "seleccionar" },
            { "case", "caso" },
            { "break", "romper" },
            { "while", "mientras" },
            { "do", "hacer" },
        };

        private static readonly Dictionary<KeywordType, VariableType> keywordTypeToVariableType = new Dictionary<KeywordType, VariableType>
        {
            { KeywordType.Int, VariableType.Int },
            { KeywordType.Float, VariableType.Float },
            { KeywordType.Bool, VariableType.Bool },
            { KeywordType.Void, VariableType.Void },
            { KeywordType.Include, VariableType.Include },
            { KeywordType.String, VariableType.String },
            { KeywordType.Char, VariableType.Char },
            { KeywordType.CharPointer, VariableType.CharPointer },
            { KeywordType.Return, VariableType.Return },
            { KeywordType.True, VariableType.True },
            { KeywordType.False, VariableType.False }
        };

        public KeywordType KeywordType { get; private set; }

        /// <summary>
        /// Devuelve verdadero si esta palabra clave es una palabra clave
        /// para un tipo, falso en caso contrario.
        /// </summary>
        public bool IsTypeKeyword
        {
            get { return keywordTypeToVariableType.ContainsKey(KeywordType); }
        }

        public KeywordToken(string content, int line, int column) : base(content, line, column)
        {
            if (!validKeywords.ContainsKey(content))
                throw new ArgumentException($"El contenido no es una palabra clave válida: {content}", "content");

            KeywordType = validKeywords[content];
        }

        /// <summary>
        /// Devuelve verdadero si la cadena dada es una palabra clave conocida,
        /// falso en caso contrario.
        /// </summary>
        public static bool IsKeyword(string s)
        {
            return validKeywords.ContainsKey(s);
        }

        /// <summary>
        /// Devuelve la versión traducida de la palabra clave, si existe.
        /// </summary>
        public static string GetTranslatedKeyword(string s)
        {
            if (ParserGlobal.IsTranslate)
            {
                try
                {
                    return validKeywordsTranslations[s];
                }
                catch
                {
                    return s;
                }
            }
            return s.ToLowerInvariant();
        }

        /// <summary>
        /// Devuelve el tipo de variable asociado a esta palabra clave,
        /// si esta palabra clave representa un tipo de variable.
        /// Lanza una excepción en caso contrario.
        /// </summary>
        public VariableType ToVariableType()
        {
            return keywordTypeToVariableType[KeywordType];
        }
 
    }
}
