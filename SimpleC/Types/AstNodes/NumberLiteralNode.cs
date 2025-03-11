namespace SimpleC.Types.AstNodes
{
    internal class NumberLiteralNode : StatementSequenceNode
    {
        public List<Token> Values { get; }

        public NumberLiteralNode(List<Token> values)
        {
            NameAst = "Literal Numero";
            Values = values;
            VerifySeparator(Values);
        }

        public override void Generate()
        {
            base.Generate();
            PrintValues(Values);
        }
    }
}
