namespace SimpleC.Types.Tokens
{
    abstract class BraceToken : Token
    {
        public BraceType BraceType { get; protected set; }

        public BraceToken(string content, int line, int column) : base(content, line, column)
        { }
    }
}
