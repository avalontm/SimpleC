namespace SimpleC.Types.AstNodes
{
    internal class PreprocessorNode : StatementSequenceNode
    {
        public string Identifier { get; }
        public bool IsFile { get; }
        public string Library { get; }

        public PreprocessorNode(IEnumerable<Token> tokens)
        {
            var _tokens = tokens.GetEnumerator();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Preprocessor:");
            Console.ResetColor();

            _tokens.MoveNext();

            if (!_tokens.Current.Content.Contains("#"))
                throw new ArgumentException($"Se esperaba un: #", "content");

            _tokens.MoveNext();
            Identifier = _tokens.Current.Content;

            _tokens.MoveNext();

            if (_tokens.Current.Content.Contains("\""))
            {
                IsFile = true;
            }
            _tokens.MoveNext();
            Library = _tokens.Current.Content;


            //Esperamos hasta movernos hasta el final del los tokens.
            while (!_tokens.MoveNext()) ;

            Console.WriteLine(this.ToString() + "\n");
        }

        public override string ToString()
        {
            string Content = $"#{Identifier} <{Library}>";

            if (IsFile)
            {
                Content = $"#{Identifier} \"{Library}\"";
            }

            return Content;
        }
    }
}
