using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    internal class KeywordNode : StatementSequenceNode
    {
        public VariableType Variable { get; }
        public string Value { get; }

        public KeywordNode(VariableType varType, string name)
        {
            NameAst = $"Palabra clave: {name}";
            Variable = varType;
            Value = name;
        }
    }
}
