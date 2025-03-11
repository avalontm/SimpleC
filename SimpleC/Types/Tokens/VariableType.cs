using SimpleC.Parsing;
using System.Diagnostics;

namespace SimpleC.Types.Tokens
{
    public enum VariableType
    {
        Int,
        Void,
        Include,
        Char,
        Float,
        Bool,
        String,
        CharPointer,
        CharArray,
        Return,
        True,
        False,
        Break,
        If
    }

    public static class VariableTypeExtensions
    {
        private static readonly Dictionary<VariableType, string> Translations = new()
        {
            { VariableType.Int, "entero" },
            { VariableType.Void, "metodo" },
            { VariableType.Include, "incluir" },
            { VariableType.Char, "carácter" },
            { VariableType.Float, "flotante" },
            { VariableType.Bool, "booleano" },
            { VariableType.String, "cadena" },
            { VariableType.CharPointer, "puntero_caracter" },
            { VariableType.CharArray, "arreglo_caracteres" },
            { VariableType.Return, "retorno" },
            { VariableType.True, "verdadero" },
            { VariableType.False, "falso" }
        };

        public static string ToLowerString(this VariableType type)
        {
            if(ParserGlobal.IsTranslate)
            {
                try
                {
                    return Translations[type];
                }
                catch
                {
                    return type.ToString().ToLower();
                }
            }
            return type.ToString().ToLower();
        }
    }
}
