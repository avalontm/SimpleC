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

        public void SetIndent(string indent)
        {
            Indentation = indent;
        }

        public virtual void Generate()
        {
            if (string.IsNullOrEmpty(Indentation))
            {
                Indentation = new string(' ', (Indent * 4));
            }
        }

    }
}
