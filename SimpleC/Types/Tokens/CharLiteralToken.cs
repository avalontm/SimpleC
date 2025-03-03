namespace SimpleC.Types.Tokens
{
    class CharLiteralToken : Token
    {
        public string CharValue { get; }

        public CharLiteralToken(string content, int line, int column) : base(content, line, column)
        {
            CharValue = content;
        }
    }

}
