namespace SimpleC.Types.AstNodes
{
    internal class OperatorNode : StatementSequenceNode
    {
        public Token Value { get; }

        public OperatorNode(Token value)
        {
            NameAst = $"Operador: {value.Content}";
            Value = value;
        }
    }
}
