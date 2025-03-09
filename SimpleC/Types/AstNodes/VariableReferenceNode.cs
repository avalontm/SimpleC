using SimpleC.Types;
using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    public class VariableReferenceNode : StatementSequenceNode
    {
        public string Name { get; private set; }
        public VariableType Type { get; private set; }

        public VariableReferenceNode(string name, VariableType type) : base()
        {
            Name = name;
            Type = type;
        }
    }
}