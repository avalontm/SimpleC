using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    // Nodo para representar bloques de código entre llaves {}
    public class BlockNode : StatementSequenceNode
    {
        public BlockNode()
        {
            
        }

        public void SetParameters(List<ParameterNode> parameters)
        {
            foreach(var parameter in parameters)
            {
                this.Register(parameter.Name, parameter.Type);
            }

        }

        public override void Generate()
        {
            base.Generate();
            ColorParser.WriteLine($@"{Indentation}[color=yellow]{{[/color]");

            foreach (var node in this.SubNodes)
            {
                node.Indent = Indent + 1;
                node.Generate();
            }

            ColorParser.WriteLine(@$"{Indentation}[color=yellow]}}[/color]");
        }
    }

}