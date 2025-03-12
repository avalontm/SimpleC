using SimpleC.Types.Tokens;
using SimpleC.Utils;
using System.Diagnostics;
namespace SimpleC.Types.AstNodes
{
    internal class ReturnNode : StatementSequenceNode
    {
        public List<Token> Values { get; }
        public ReturnNode(List<Token> tokens)
        {
            NameAst = "Regresar";
            Values = new List<Token>();
            bool hasSemicolon = false;
            bool hasOtherValues = false;

            foreach (var token in tokens)
            {
                if (token is not KeywordToken keywordToken)
                {
                    // Verificar si es un punto y coma (token de cierre)
                    if (token.Content == ";")
                    {
                        hasSemicolon = true;
                    }
                    else
                    {
                        hasOtherValues = true;
                    }

                    Values.Add(token);
                }
            }

            // Verificar que tenga el token de cierre (;)
            if (!hasSemicolon)
            {
                var lastToken = tokens.LastOrDefault();
                int line = lastToken?.Line ?? 0;
                int column = lastToken?.Column ?? 0;
                throw new Exception($"Error: Falta el punto y coma (;) al final del retorno: Linea {line}, Columna {column}.");
            }

            // Verificar que haya al menos un valor además del punto y coma
            if (!hasOtherValues)
            {
                // Obtener la línea y columna del token 'return'
                var returnToken = tokens.FirstOrDefault(t => t is KeywordToken);
                int line = returnToken?.Line ?? 0;
                int column = returnToken?.Column ?? 0;
                throw new Exception($"Error: Declaración de retorno sin valor: Linea {line}, Columna {column}. Se necesita retornar al menos un valor.");
            }
        }

        public void SetOwner(StatementSequenceNode node)
        {
            foreach (var token in Values)
            {
                if (token is IdentifierToken identifierToken)
                {
                    if (!node.Verify(identifierToken.Content))
                    {
                        throw new Exception($"La variable `{token.Content}` no se encontro: Linea {token.Line}, Columna {token.Column}.");
                    }
                }
            }
        }

        public override void Generate()
        {
            base.Generate();
            List<string> values = new List<string>();
            foreach (var value in Values)
            {
                if (value is not KeywordToken keywordToken)
                {
                    values.Add(ColorParser.GetTokenColor(value));
                }
            }
            ColorParser.WriteLine($"{Indentation}[color=magenta]{KeywordToken.GetTranslatedKeyword("return")}[/color] {string.Join(" ", values)}");
        }
    }
}