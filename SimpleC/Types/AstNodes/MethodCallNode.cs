using SimpleC.Types.Tokens;
using System.Diagnostics;

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

            Debug.WriteLine($"{Indentation}{Name} {string.Join(" ", arguments.Select(x=>x.Content))}");
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
