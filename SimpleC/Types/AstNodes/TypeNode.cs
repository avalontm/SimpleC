using SimpleC.CodeGeneration;
using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    class TypeNode : ExpressionNode
    {
        public KeywordType Type { get; }

        public TypeNode(KeywordType type)
        {
            Type = type;
        }

        public override void EmitCode(CodeEmitter emitter)
        {

        }
    }

}
