namespace SimpleC.Types.AstNodes
{
    public class AssignmentNode : StatementSequenceNode
    {
        public Token VariableName { get; private set; }
        public List<Token> Value { get; private set; }

        public AssignmentNode(Token variableName, List<Token> value) : base()
        {
            NameAst = $"Asignación: {variableName.Content}";
            VariableName = variableName;
            Value = value;
        }
    }
}
