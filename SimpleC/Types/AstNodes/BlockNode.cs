using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    // Nodo para representar bloques de código entre llaves {}
    public class BlockNode : StatementSequenceNode
    {
        public MethodNode? Owner { get; private set; }
        public Token OpenBraceToken { get; set; }
        public Token CloseBraceToken { get; set; }
        public bool IsControlFlowBlock { get; set; }

        private List<ParameterNode> Parameters = new List<ParameterNode>();

        public BlockNode() : base()
        {
            NameAst = "Bloque";
        }

        public void SetParameters(MethodNode owner, List<ParameterNode> parameters)
        {
            this.Owner = owner;
            this.Parameters = parameters;

            foreach (var parameter in parameters)
            {
                this.Register(parameter.Value, parameter.Type);
            }
        }

        public List<ParameterNode> GetParameters()
        {
            return Parameters;
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
                node.Generate();
                Debug.WriteLine(node.NameAst);
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
    }

}