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
                if (token is not StatementSperatorToken)
                {
                    Values.Add(token);
                }
            }

            ColorParser.WriteLine(this.ToString());
        }

        public override string ToString()
        {
            List<string> values = new List<string>();

            foreach (var value in Values)
            {
                if(value is not KeywordToken keywordToken)
                {
                    values.Add(ColorParser.GetTokenColor(value));
                }
               
            }
            return $"[color=magenta]return[/color] {string.Join(" ", values)}";
        }
    }
}
