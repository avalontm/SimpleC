using SimpleC.CodeGeneration;

namespace SimpleC.Types.AstNodes
{
    class IncludeDirectiveNode : AstNode
    {
        public string FileName { get; }

        public IncludeDirectiveNode(string fileName)
        {
            FileName = fileName;
        }

        public override void EmitCode(CodeEmitter emitter)
        {
            // Implementa la lógica de emisión de código para la directiva #include si es necesario
        }
    }

}
