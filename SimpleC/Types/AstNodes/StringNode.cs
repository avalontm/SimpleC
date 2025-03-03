namespace SimpleC.Types.AstNodes
{
    internal class StringNode : StatementSequenceNode
    {
        public string Value;

        public StringNode(string value)
        {
            Value = value;

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Cadena: \"{Value}\"");
            Console.ResetColor();
        }
    }
}
