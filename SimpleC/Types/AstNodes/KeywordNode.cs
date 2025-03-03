using SimpleC.CodeGeneration;
using SimpleC.Types.AstNodes;
using SimpleC.Types.Tokens;

namespace SimpleC.Types
{
    /// <summary>
    /// Nodo que representa una palabra clave en el AST.
    /// </summary>
    class KeywordNode : ExpressionNode
    {
        public string Keyword { get; }

        public KeywordNode(string keyword)
        {
            Keyword = keyword;
        }

        public override void EmitCode(CodeEmitter emitter)
        {
     
        }
    }
}
