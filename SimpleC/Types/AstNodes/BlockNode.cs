namespace SimpleC.Types.AstNodes
{
    // Nodo para representar bloques de código entre llaves {}
    public class BlockNode : StatementSequenceNode
    {
        public BlockNode() : base()
        {

        }

        public override void Generate()
        {
            ColorParser.WriteLine(@"[color=yellow]{[/color]");

            foreach (var node in this.SubNodes)
            {
                node.Indent = 1;
                node.Generate();
            }

            ColorParser.WriteLine(@"[color=yellow]}[/color]");
        }
    }

}