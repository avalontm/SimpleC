using SimpleC.Parsing;
using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    internal class StringNode : StatementSequenceNode
    {
        public Token Value;

        public StringNode(Token value)
        {
            NameAst = "Cadena de texto";
            Value = value;

            if(ParserGlobal.IsTranslate)
            {
                Value.Content= KeywordToken.GetTranslatedKeyword(Value.Content);
            }
        }

        public override void Generate()
        {
            base.Generate();

            ColorParser.WriteLine($"\"{ColorParser.GetTokenColor(Value)}\"");
        }
    }
}
