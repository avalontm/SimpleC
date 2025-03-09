using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    public class MethodCallNode : StatementSequenceNode
    {
        public VariableType ReturnType { get; private set; }
        public string Name { get; private set; }
        public List<Token> Arguments { get; private set; }

        public MethodCallNode(VariableType returnType, string name, List<Token> arguments) : base()
        {
            ReturnType = returnType;
            Name = name;
            Arguments = arguments;

            ColorParser.WriteLine(this.ToString());
        }

        public override string ToString()
        {
            List<string> arguments = new List<string>();

            foreach (var arg in Arguments)
            {
                arguments.Add(ColorParser.GetTokenColor(arg));
            }

            return $"[color=yellow]{Name}[/color] {string.Join(" ", arguments)}";
        }
    }
}
