using SimpleC.Types.Tokens;

namespace SimpleC.Types
{
    class CommentToken : Token
    {
        public string Content { get; }

        public CommentToken(string content) : base(content)
        {
            Content = content;
        }

        public override string ToString()
        {
            return $"// {Content}";
        }
    }
}
