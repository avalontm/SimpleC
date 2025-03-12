
namespace SimpleC.Types.AstNodes
{
    internal class ProgramNode : StatementSequenceNode
    {
        public ProgramNode() {
            NameAst = "Programa Principal";
        }

        public override List<byte> ByteCode()
        {
            return base.ByteCode();
        }
    }
}
