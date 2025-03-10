using SimpleC.Parsing;

namespace SimpleC.Types
{
    public abstract class AstNode
    {
        public string Indentation { get; private set; }
        public int Indent { set; get; } = 0;

        public AstNode()
        {
            Indentation = string.Empty;
        }

        public virtual void Generate()
        {
            Indentation = new string(' ', (Indent*4));
        }

    }
}
