namespace SimpleC.Types
{
    public abstract class Token
    {
        public string Content { get; set; }
        public int Line { get; }
        public int Column { get; }

        public Token(string content, int line, int column)
        {
            this.Content = content;
            this.Line = line;
            this.Column = column;
        }

        public Token(int line, int column)
        {

            this.Line = line;
            this.Column = column;
        }

        public override string ToString()
        {
            return string.Format("[{0}] - {1} (Línea: {2}, Columna: {3})", this.GetType().Name, Content, Line, Column);
        }
    }
}
