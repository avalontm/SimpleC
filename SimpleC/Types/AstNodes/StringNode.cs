using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    internal class StringNode : StatementSequenceNode
    {
        public List<Token> Values;

        public StringNode(List<Token> values)
        {
            NameAst = "Cadena de texto";
            Values = values;

            VerifySeparator(values);
        }

        public override void Generate()
        {
            base.Generate();
            PrintValues(Values);
        }
    }
}
