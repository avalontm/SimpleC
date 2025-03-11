using SimpleC.Parsing;
using SimpleC.Types.Tokens;

namespace SimpleC.Types.AstNodes
{
    internal class PreprocessorNode : StatementSequenceNode
    {
        public string Identifier { get; }
        public Token Library { get; }
        public bool IsFile { get; }

        public PreprocessorNode(IEnumerable<Token> tokens)
        {
            NameAst = "Preprocesador: Incluir";
            var _tokens = tokens.GetEnumerator();

            _tokens.MoveNext();

            if (!_tokens.Current.Content.Contains("#"))
                throw new ArgumentException($"Se esperaba un: #", "content");

            _tokens.MoveNext();
            Identifier = _tokens.Current.Content;

            if(ParserGlobal.IsTranslate)
            {
                Identifier  = KeywordToken.GetTranslatedKeyword(Identifier);
            }
            _tokens.MoveNext();

            if (_tokens.Current.Content.Contains("\""))
            {
                IsFile = true;
            }
            _tokens.MoveNext();

            if (string.IsNullOrEmpty(_tokens.Current.Content))
            {
                throw new ArgumentException("Se esperaba un nombre de la libraria.");
            }

            Library = _tokens.Current;

            //Esperamos hasta movernos hasta el final del los tokens.
            while (!_tokens.MoveNext()) ;

        }

        public override void Generate()
        {
            base.Generate();

            string Content = $"{Indentation}[color=magenta]#{Identifier}[/color] [color=orange]<[/color]{ColorParser.GetTokenColor(Library)}[color=orange]>[/color]";

            if (IsFile)
            {
                Content = $"{Indentation}[color=magenta]#{Identifier}[/color] [color=orange]\"[/color]{ColorParser.GetTokenColor(Library)}[color=orange]\"[/color]";
            }

            ColorParser.WriteLine(Content);
        }
    }
}
