namespace SimpleC.Types.AstNodes
{
    internal class StatementSperatorNode : StatementSequenceNode
    {
        public Token Value { get; }

        public StatementSperatorNode(Token value)
        {
            Value = value;
        }

        public override void Generate()
        {
            base.Generate();

            if (Value.Content == ":")
            {
                ColorParser.WriteLine($"{Value.Content.Trim()}");
            }
        }
    }
}
