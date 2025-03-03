using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class DecimalLiteralNode : ExpressionNode
    {
        public float Value { get; private set; } // Cambiamos int por double

        public DecimalLiteralNode(float value) // Cambiamos el parámetro a double
        {
            Value = value;
        }

        public override void EmitCode(CodeEmitter emitter)
        {
           
        }
    }
}

