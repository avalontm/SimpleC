using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    internal class PreprocessorNode : StatementSequenceNode
    {
        public string Identifier { get; }
        public Token Library { get; }
        public bool IsFile { get; }

        public PreprocessorNode(List<Token> tokens)
        {
            NameAst = "Preprocesador: Incluir";

            Debug.WriteLine("");
            // Primer token debe ser un '#'
            if (tokens[0].Content != "#")
                throw new ArgumentException($"Se esperaba un '#', pero se encontró '{tokens[0].Content}'", nameof(tokens));

            Identifier = tokens[1].Content;

            // Traducción del identificador si es necesario
            if (ParserGlobal.IsTranslate)
            {
                Identifier = KeywordToken.GetTranslatedKeyword(Identifier);
            }

            // Verificación del delimitador de apertura '<' o '"'
            if (tokens[2].Content != "<" && tokens[2].Content != "\"")
                throw new ArgumentException($"Se esperaba '<' o '\"' después de '{Identifier}', pero se encontró '{tokens[2].Content}'", nameof(tokens));

            char openingChar = tokens[2].Content[0]; // Guardar el delimitador de apertura
            char closingChar = (openingChar == '<') ? '>' : '"';
            IsFile = (openingChar == '"');

            // Validar el nombre de la librería
            if (string.IsNullOrEmpty(tokens[3].Content) || tokens[3].Content == closingChar.ToString())
                throw new ArgumentException("Se esperaba el nombre de la librería, pero no se proporcionó.", nameof(tokens));

            Library = tokens[3];

            // Verificación del delimitador de cierre
            if (tokens.Count < 5 || tokens[4].Content != closingChar.ToString())
                throw new ArgumentException($"Falta el delimitador de cierre '{closingChar}', o hay tokens adicionales no esperados.", nameof(tokens));
        }

        public override void Generate()
        {
            base.Generate();

            string content = $"{Indentation}[color=magenta]#{Identifier}[/color] ";
            if (IsFile)
            {
                content += $"[color=orange]\"[/color]{ColorParser.GetTokenColor(Library)}[color=orange]\"[/color]";
            }
            else
            {
                content += $"[color=orange]<[/color]{ColorParser.GetTokenColor(Library)}[color=orange]>[/color]";
            }

            ColorParser.WriteLine(content);
        }
    }
}
