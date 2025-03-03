using SimpleC.Types.Tokens;

namespace SimpleC.Types
{
    class DecimalLiteralToken : Token
    {
        public float Number { get; }

        public DecimalLiteralToken(string number) : base(number)
        {
            Number = float.Parse(number, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
