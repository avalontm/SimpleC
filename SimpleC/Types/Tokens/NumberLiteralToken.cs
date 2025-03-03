namespace SimpleC.Types.Tokens
{
    class NumberLiteralToken : Token
    {
        public double Number { get; } // Propiedad de solo lectura

        public NumberLiteralToken(string content, int line, int column) : base(content, line, column)
        {
            if (!double.TryParse(content, out double number))
                throw new ArgumentException("The content is not a valid number.", nameof(content));

            Number = number;
        }
    }
}
