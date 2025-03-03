using SimpleC.Types.Tokens;

namespace SimpleC.Types
{
    class CommentToken : Token
    {
        public CommentToken(string content, int line, int column) : base(content, line, column)
        {

        }

        public override string ToString()
        {
            return $"// {Content}";
        }
    }
}
