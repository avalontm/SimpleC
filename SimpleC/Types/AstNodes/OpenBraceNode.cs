namespace SimpleC.Types.AstNodes
{
    internal class OpenBraceNode : StatementSequenceNode
    {
        public string Value { get; }

        public OpenBraceNode(string value)
        {
            Value = value;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"OpenBrace: {Value}");
            Console.ResetColor();
        }
    }
}
