using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    internal class KeywordNode : StatementSequenceNode
    {
        public VariableType Variable { get; }
        public string Name { get; }

        public KeywordNode(VariableType varType, string name)
        {
            Variable = varType;
            Name = name;

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Keyword: {Variable} {Name}");
            Console.ResetColor();
        }
    }
}
