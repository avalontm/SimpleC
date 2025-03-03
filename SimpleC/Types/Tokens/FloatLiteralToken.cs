using SimpleC.Types.Tokens;

namespace SimpleC.Types
{
    class FloatLiteralToken : Token
    {
        public float Number { get; }

        public FloatLiteralToken(string number) : base(number)
        {
            Number = float.Parse(number, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
