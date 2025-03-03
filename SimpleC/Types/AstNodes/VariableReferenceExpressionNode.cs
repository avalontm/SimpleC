using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class VariableReferenceExpressionNode : ExpressionNode
    {
        public string VariableName { get; private set; }

        public VariableReferenceExpressionNode(string varName)
        {
            VariableName = varName;
        }

        public override void EmitCode(CodeEmitter emitter)
        {

        }
    }
}
