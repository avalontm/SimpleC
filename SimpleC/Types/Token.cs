namespace SimpleC.Types
{
    abstract class Token
    {
        public string Content { get; private set; }

        public Token(string content)
        {
            this.Content = content;
        }

        public override string ToString()
        {
            return string.Format("[{0}] - {1}", this.GetType().Name, Content);
        }
    }
}
