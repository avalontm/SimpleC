using SimpleC.Types;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    public class VariableReferenceNode : StatementSequenceNode
    {
        public Token Value { get; private set; }
        public VariableType Type { get; private set; }

        public VariableReferenceNode(Token value, VariableType type) : base()
        {
            NameAst = $"Referencia: {value.Content}";
            Type = type;
            Value = value;
        }
    }
}