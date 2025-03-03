namespace SimpleC.Types.Tokens
{
    abstract class BraceToken : Token
    {
        public BraceType BraceType { get; protected set; }

        public BraceToken(string content) : base(content)
        { }
    }
}
