using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    internal class KeywordNode : StatementSequenceNode
    {
        public VariableType Variable { get; }
        public string Value { get; }

        public KeywordNode(VariableType varType, string name)
        {
            NameAst = $"Palabra clave: {name}";
            Variable = varType;
            Value = name;
        }

        public override void Generate()
        {
            base.Generate();
            ColorParser.WriteLine($"[color=blue]{Value}[/color]");
        }
    }
}
