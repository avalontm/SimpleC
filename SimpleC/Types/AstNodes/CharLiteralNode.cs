namespace SimpleC.Types.AstNodes
{
    internal class CharLiteralNode : StatementSequenceNode
    {
        public char Value { get; }

        public CharLiteralNode(char value)
        {
            NameAst = "Literal Carácter";
            Value = value;

        }
    }
}
