using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    /// <summary>
    /// Nodo que representa todo el programa.
    /// Este debe ser el punto de inicio del AST.
    /// </summary>
    class ProgramNode : StatementSequenceNode
    {
        public override void EmitCode(CodeEmitter emitter)
        {
         
        }
    }
}
