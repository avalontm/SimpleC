using SimpleC.Types.Tokens;
using System.Diagnostics;

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
                if (token is not KeywordToken keywordToken)
                {
                    Values.Add(token);
                }
            }
        }

        public void SetOwner(StatementSequenceNode node)
        {
            foreach (var token in Values)
            {
                if (token is IdentifierToken identifierToken)
                {
                    Debug.WriteLine($"identifier: {identifierToken.Content}");

                    if (!node.Verify(identifierToken.Content))
                    {
                        throw new Exception($"La variable `{token.Content}` no se encontro: Linea {token.Line}, Columna {token.Column}.");
                    }
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
