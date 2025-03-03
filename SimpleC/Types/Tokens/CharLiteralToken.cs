namespace SimpleC.Types.Tokens
{
    class CharLiteralToken : Token
    {
        public char Value { get; }

        public CharLiteralToken(char value) : base(value.ToString())
        {
            Value = value;
        }
    }

}
