namespace SimpleC.Types.AstNodes
{
    internal class FloatLiteralNode : StatementSequenceNode
    {
        public Token Value { get; }

        public FloatLiteralNode(Token value)
        {
            NameAst = "Literal Flotante";
            Value = value;
        }

        public override void Generate()
        {
            base.Generate();

            ColorParser.WriteLine(ColorParser.GetTokenColor(Value));
        }
    }
}
