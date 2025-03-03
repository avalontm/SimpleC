namespace SimpleC.Types.AstNodes
{
    internal class StatementSperatorNode : StatementSequenceNode
    {
        public string Value { get; }

        public StatementSperatorNode(string value)
        {
            Value = value;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Separator: {Value}");
            Console.ResetColor();
        }
    }
}
