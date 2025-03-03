namespace SimpleC.Types.AstNodes
{
    internal class CloseBraceNode : StatementSequenceNode
    {
        public string Value { get; }

        public CloseBraceNode(string value)
        {
            Value = value;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"CloseBrace: {Value}");
            Console.ResetColor();
        }
    }
}
