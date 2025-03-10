using SimpleC.Parsing;

namespace SimpleC.Types.AstNodes
{
    internal class IdentifierNode : StatementSequenceNode
    {
        public string Value { get; }

        public IdentifierNode(string name)
        {
            NameAst = $"Identificador: {name}";
            Value = name;

            ParserGlobal.Register(Value, this);
        }
    }
}
