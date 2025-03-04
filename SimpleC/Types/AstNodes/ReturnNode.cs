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
                if (token.Current != null)
                {
                    if (!token.Current.Content.Contains(";"))
                    {
                        Value += $"{token.Current.Content} ";
                    }
                }
            }

            Console.WriteLine(this.ToString());
            Generate();
        }

        public override void Generate()
        {
            LLVMSharp.GenerateReturn(Value);
        }


        public override string ToString()
        {
            return $"Return {Value}";
        }
    }
}
