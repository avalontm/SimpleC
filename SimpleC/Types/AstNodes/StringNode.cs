namespace SimpleC.Types.AstNodes
{
    internal class StringNode : StatementSequenceNode
    {
        public Token Value;

        public StringNode(Token value)
        {
            NameAst = "Cadena de texto";
            Value = value;
        }

        public override void Generate()
        {
            base.Generate();

            ColorParser.WriteLine($"\"{ColorParser.GetTokenColor(Value)}\"");
        }
    }
}
