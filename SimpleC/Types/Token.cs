namespace SimpleC.Types
{
    abstract class Token
    {
        public string Content { get; private set; }
        public int Line { get; }
        public int Column { get; }

        public Token(string content, int line, int column)
        {
            this.Content = content;
            this.Line = line;
            this.Column = column;
        }

        public override string ToString()
        {
            return string.Format("[{0}] - {1} (Línea: {2}, Columna: {3})", this.GetType().Name, Content, Line, Column);
        }
    }
}
