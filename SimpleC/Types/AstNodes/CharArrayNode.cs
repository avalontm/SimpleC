using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class CharArrayNode : ExpressionNode
    {
        public string Value { get; }
        public int? ArraySize { get; }

        public CharArrayNode(string value, int? arraySize)
        {
            Value = value;
            ArraySize = arraySize;
        }

        public override void EmitCode(CodeEmitter emitter)
        {
        
        }
    }
}
