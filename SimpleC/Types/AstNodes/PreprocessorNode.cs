using LLVMSharp.Interop;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    internal class PreprocessorNode : StatementSequenceNode
    {
        public string Identifier { get; }
        public Token Library { get; }
        public bool IsFile { get; }
       
        public PreprocessorNode(IEnumerable<Token> tokens)
        {
            var _tokens = tokens.GetEnumerator();

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

            if(string.IsNullOrEmpty(_tokens.Current.Content))
            {
                throw new ArgumentException("Se esperaba un nombre de la libraria.");
            }

            Library = _tokens.Current;

            //Esperamos hasta movernos hasta el final del los tokens.
            while (!_tokens.MoveNext())
            {
                Debug.WriteLine($"token: {_tokens.Current}");
            }

            ColorParser.WriteLine(this.ToString());
        }



        public override string ToString()
        {
            string Content = $"[color=magenta]#{Identifier}[/color] [color=orange]<[/color]{ColorParser.GetTokenColor(Library)}[color=orange]>[/color]";

            if (IsFile)
            {
                Content = $"[color=magenta]#{Identifier}[/color] [color=orange]\"[/color]{ColorParser.GetTokenColor(Library)}[color=orange]\"[/color]";
            }

            return Content;
        }
    }
}
