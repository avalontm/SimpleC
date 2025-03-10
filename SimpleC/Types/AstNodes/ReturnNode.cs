using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    internal class ReturnNode : StatementSequenceNode
    {
        public List<Token> Values { get; }

        public ReturnNode(List<Token> tokens)
        {
            Values = new List<Token>();

            foreach (var token in tokens)
            {
                if (token is not StatementSperatorToken)
                {
                    Values.Add(token);
                }
            }

        }

        public override void Generate()
        {
            base.Generate();
            List<string> values = new List<string>();

            foreach (var value in Values)
            {
                if (value is not KeywordToken keywordToken)
                {
                    values.Add(ColorParser.GetTokenColor(value));
                }

            }
            ColorParser.WriteLine($"{Indentation}[color=magenta]return[/color] {string.Join(" ", values)}");
        }
    }
}
