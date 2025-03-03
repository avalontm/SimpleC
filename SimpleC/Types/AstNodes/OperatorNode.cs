namespace SimpleC.Types.AstNodes
{
    internal class OperatorNode : StatementSequenceNode
    {
        public string Value { get; }

        public OperatorNode(string value)
        {
            Value = value;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Operador: {Value}");
            Console.ResetColor();
        }
    }
}
