namespace SimpleC.Types.Tokens
{
    class CharLiteralToken : Token
    {
        public string CharValue { get; }

        public CharLiteralToken(char value) : base(value.ToString())
        {
            CharValue = value.ToString();
        }
    }

}
