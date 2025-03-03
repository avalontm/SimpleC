using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class BlockStatementNode : StatementSequenceNode
    {
        public List<AstNode> Statements { get; private set; } = new List<AstNode>();

        public override void EmitCode(CodeEmitter emitter)
        {
            emitter.Emit(OpCode.Enter); // Indicar inicio de bloque

            foreach (var statement in Statements)
            {
                statement.EmitCode(emitter);
            }

            emitter.Emit(OpCode.Return); // Indicar fin de bloque
        }
    }

}
