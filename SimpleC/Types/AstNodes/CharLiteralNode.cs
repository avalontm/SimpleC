using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class CharLiteralNode : ExpressionNode
    {
        public string Value { get; }

        public CharLiteralNode(string value)
        {
            Value = value;
        }

        public override void EmitCode(CodeEmitter emitter)
        {

        }
    }
}
