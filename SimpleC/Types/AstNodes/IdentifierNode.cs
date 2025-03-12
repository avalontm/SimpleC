using SimpleC.Parsing;
using SimpleC.Utils;

namespace SimpleC.Types.AstNodes
{
    internal class IdentifierNode : StatementSequenceNode
    {
        public string Value { get; }

        public IdentifierNode(string name)
        {
            NameAst = $"Identificador: {name}";
            Value = name;

            ParserGlobal.Register(Value, this);
        }

        public override void Generate()
        {
            base.Generate();
            ColorParser.WriteLine($"[color=cyan]{Value}[/color]");
        }
    }
}
