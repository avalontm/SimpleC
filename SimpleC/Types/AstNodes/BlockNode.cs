namespace SimpleC.Types.AstNodes
{
    // Nodo para representar bloques de código entre llaves {}
    public class BlockNode : StatementSequenceNode
    {
        Token Value { get; }
        public BlockNode(Token value) : base()
        {
            Value = value;
            ColorParser.WriteLine(this.ToString());
        }

        public override string ToString()
        {
            return $"{ColorParser.GetTokenColor(Value)}";
        }
    }

}