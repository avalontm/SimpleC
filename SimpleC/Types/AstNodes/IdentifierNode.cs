using SimpleC.Parsing;

namespace SimpleC.Types.AstNodes
{
    internal class IdentifierNode : StatementSequenceNode
    {
        public string Name { get; }

        public IdentifierNode(string name)
        {
            Name = name;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Identificador: {Name}");
            Console.ResetColor();

            ParserGlobal.Register(Name, this);
        }
    }
}
