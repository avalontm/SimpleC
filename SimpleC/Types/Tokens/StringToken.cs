namespace SimpleC.Types.Tokens
{
    class StringToken : Token
    {
        public string Content { get; private set; }

        public StringToken(string content) : base(content) 
        { 
            Content = content;
        }
    }

}
