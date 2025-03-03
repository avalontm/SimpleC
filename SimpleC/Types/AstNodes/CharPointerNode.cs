using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class CharPointerNode : ExpressionNode
    {
        public string Value { get; }

        public CharPointerNode(string value)
        {
            Value = value;
        }

        public override void EmitCode(CodeEmitter emitter)
        {
    
        }
    }
}
