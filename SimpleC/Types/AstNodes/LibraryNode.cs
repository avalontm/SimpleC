using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class LibraryNode : ExpressionNode
    {
        public string Name { get; }

        public LibraryNode(string name)
        {
            Name = name;
        }

        public override void EmitCode(CodeEmitter emitter)
        {
            // Implementación futura, si es necesario
        }
    }
}
