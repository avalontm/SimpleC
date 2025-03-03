namespace SimpleC.Types.Tokens
{
    class StringToken : Token
    {
        public string StringValue { get; private set; }

        public StringToken(string content) : base(content) 
        {
            StringValue = content;
        }
    }

}
