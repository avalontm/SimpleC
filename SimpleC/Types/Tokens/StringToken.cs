namespace SimpleC.Types.Tokens
{
    class StringToken : Token
    {

        public StringToken(string content, int line, int column) : base(line, column)
        {
            Content = $"\"{content}\"";
        }
    }

}
