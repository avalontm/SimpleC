using SimpleC.CodeGeneration;
using SimpleC.Types.AstNodes;

namespace SimpleC.Types
{
    class StringLiteralNode : ExpressionNode
    {
        public string Value { get; }

        public StringLiteralNode(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return $"\"{Value}\"";
        }

        public override void EmitCode(CodeEmitter emitter)
        {
         
        }
    }
}
