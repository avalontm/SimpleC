using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class NumberLiteralNode : ExpressionNode
    {
        public double Value { get; private set; } // Cambiamos int por double

        public NumberLiteralNode(double value) // Cambiamos el parámetro a double
        {
            Value = value;
        }

        public override void EmitCode(CodeEmitter emitter)
        {
           
        }
    }
}
