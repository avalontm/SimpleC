namespace SimpleC.Types.AstNodes
{
    public class AssignmentNode : StatementSequenceNode
    {
        public string VariableName { get; private set; }
        public List<Token> Value { get; private set; }

        public AssignmentNode(string variableName, List<Token> value) : base()
        {
            VariableName = variableName;
            Value = value;
        }
    }
}
