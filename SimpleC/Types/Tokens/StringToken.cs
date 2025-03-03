namespace SimpleC.Types.Tokens
{
    class StringToken : Token
    {
        public string StringValue { get; private set; }

        public StringToken(string content, int line, int column) : base(content, line, column)
        {
            StringValue = content;
        }
    }

}
