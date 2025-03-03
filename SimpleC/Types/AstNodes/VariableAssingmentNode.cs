using SimpleC.CodeGeneration;
using SimpleC.Parsing;

namespace SimpleC.Types.AstNodes
{
    class VariableAssingmentNode : AstNode
    {
        public string VariableName { get; private set; }
        public ExpressionNode ValueExpression { get; private set; }

        public VariableAssingmentNode(string name, ExpressionNode expr)
        {
            if (expr == null)
                throw new ParsingException("¡La expresión asignada no puede ser nula!");

            VariableName = name;
            ValueExpression = expr;
        }

        public override void EmitCode(CodeEmitter emitter)
        {

        }
    }
}
