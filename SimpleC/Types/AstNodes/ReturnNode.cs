namespace SimpleC.Types.AstNodes
{
    internal class ReturnNode : StatementSequenceNode
    {
        public string Value { get; }

        public ReturnNode(List<Token> tokens)
        {
            var token = tokens.GetEnumerator();

            while (token.MoveNext())
            {
                Value += $"{token.Current.Content} ";
            }

            Console.WriteLine(this.ToString());
        }

        public override string ToString()
        {
            return $"Return {Value}";
        }
    }
}
