using SimpleC.Types.Tokens;
using SimpleC.Utils;
using SimpleC.VM;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    // Nodo para representar bloques de código entre llaves {}
    public class BlockNode : StatementSequenceNode
    {
        public Token OpenBraceToken { get; set; }
        public Token CloseBraceToken { get; set; }
        public bool IsControlFlowBlock { get; set; }


        public BlockNode() : base()
        {
            NameAst = "Bloque";
        }

        public bool IsComplete()
        {   
   
            // Un bloque está completo si tiene tanto token de apertura como de cierre
            return OpenBraceToken != null && CloseBraceToken != null;
        }

        public override void Generate()
        {
            base.Generate();
            ColorParser.WriteLine($"{Indentation}[color=yellow]{OpenBraceToken.Content}[/color]");

            foreach (var node in this.SubNodes)
            {
                node.Indent = Indent + 1;
                node.SetOwner(Owner);
                node.Generate();
            }

            ColorParser.WriteLine($"{Indentation}[color=yellow]{CloseBraceToken.Content}[/color]");
        }

        public void HasReturn()
        {
            if (!IsControlFlowBlock)
            {
                //Verificar que el método tenga return si no es void
                if (Owner?.Type != VariableType.Void && !HasReturnStatement())
                {
                    throw new Exception($"Error: El método '{Owner?.Value}' de tipo '{Owner?.Type}' debe retornar un valor. " +
                                        $"(Línea: {OpenBraceToken.Line}, Columna: {OpenBraceToken.Column})");
                }
            }
        }

        /// <summary>
        /// Verifica si el bloque contiene al menos un return.
        /// </summary>
        public bool HasReturnStatement()
        {
            return this.SubNodes.Any(node => node is ReturnNode);
        }


        public override List<byte> ByteCode()
        {
            List<byte> byteCode = new List<byte>();

            foreach (var node in this.SubNodes)
            {
                byteCode.AddRange(node.ByteCode());
            }


            return byteCode;
        }
    }

}