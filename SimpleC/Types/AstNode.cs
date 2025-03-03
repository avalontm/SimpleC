using SimpleC.CodeGeneration;

namespace SimpleC.Types
{
    abstract class AstNode
    {
        public abstract void EmitCode(CodeEmitter emitter);
    }
}
