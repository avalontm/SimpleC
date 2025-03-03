using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class ReturnStatementNode : AstNode
    {
        /// <summary>
        /// Una expresión para el valor retornado. Puede ser nula si
        /// no se devuelve ningún valor.
        /// </summary>
        public ExpressionNode ValueExpression { get; private set; }

        public ReturnStatementNode(ExpressionNode valueExpr)
        {
            ValueExpression = valueExpr;
        }

        public override void EmitCode(CodeEmitter emitter)
        {
            if (ValueExpression != null)
            {
                ValueExpression.EmitCode(emitter); // Evalúa la expresión y la deja en la pila
                emitter.Emit(OpCode.Pop);          // Saca el resultado de la pila
                emitter.Emit(OpCode.StoreR, 0);    // Guarda en R0 (si StoreR acepta un índice)
            }

            emitter.Emit(OpCode.Return); // Devuelve el control
        }
    }
}
