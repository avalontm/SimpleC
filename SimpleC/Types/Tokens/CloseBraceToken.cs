namespace SimpleC.Types.Tokens
{
    class CloseBraceToken : BraceToken
    {
        public CloseBraceToken(string content, int line, int column) : base(content, line, column)
        {
            switch (content)
            {
                case ")":
                    BraceType = BraceType.Round;
                    break;
                case "]":
                    BraceType = BraceType.Square;
                    break;
                case "}":
                    BraceType = BraceType.Curly;
                    break;
                default:
                    throw new ArgumentException("The content is no closing brace.", "content");
            }
        }
    }
}
