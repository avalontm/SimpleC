using SimpleC.Types.Tokens;

namespace SimpleC.Types
{
    class FloatLiteralToken : Token
    {
        public float Number { get; }

        public FloatLiteralToken(string content, int line, int column) : base(content, line, column)
        {
            Number = float.Parse(content, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
