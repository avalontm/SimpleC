using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class ReturnStatementNode : AstNode
    {
        /// <summary>
        /// Una expresi�n para el valor retornado. Puede ser nula si
        /// no se devuelve ning�n valor.
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
                ValueExpression.EmitCode(emitter); // Eval�a la expresi�n y la deja en la pila
                emitter.Emit(OpCode.Pop);          // Saca el resultado de la pila
                emitter.Emit(OpCode.StoreR, 0);    // Guarda en R0 (si StoreR acepta un �ndice)
            }

            emitter.Emit(OpCode.Return); // Devuelve el control
        }
    }
}
