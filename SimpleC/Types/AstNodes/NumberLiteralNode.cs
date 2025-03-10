namespace SimpleC.Types.AstNodes
{
    internal class NumberLiteralNode : StatementSequenceNode
    {
        public Token Value { get; }

        public NumberLiteralNode(Token value)
        {
            NameAst = "Literal Numero";
            Value = value;

        }

        public override void Generate()
        {
            base.Generate();

            ColorParser.WriteLine(ColorParser.GetTokenColor(Value));
        }
    }
}
