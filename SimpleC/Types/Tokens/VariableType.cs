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
        False
    }

    public static class VariableTypeExtensions
    {
        public static string ToLowerString(this VariableType type)
        {
            return type.ToString().ToLower();
        }
    }
}
