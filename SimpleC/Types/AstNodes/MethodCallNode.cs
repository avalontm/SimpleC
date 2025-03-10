using SimpleC.Parsing;
using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    public class MethodCallNode : StatementSequenceNode
    {
        public VariableType ReturnType { get; }
        public string Name { get; private set; }
        public List<Token> Arguments { get; }

        public MethodCallNode(VariableType returnType, string name, List<Token> arguments) : base()
        {
            ReturnType = returnType;
            Name = name;
            Arguments = arguments;

        }

        public override void Generate()
        {
            base.Generate();
            List<string> arguments = new List<string>();

            foreach (var arg in Arguments)
            {
                arguments.Add(ColorParser.GetTokenColor(arg));
            }
            ColorParser.WriteLine($"{Indentation}[color=yellow]{Name}[/color] {string.Join(" ", arguments)}");
        }
    }
}
