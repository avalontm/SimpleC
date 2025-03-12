using SimpleC.Types.Tokens;
using SimpleC.Utils;

namespace SimpleC.Types
{
    public abstract class AstNode
    {
        public string NameAst { get; internal set; }
        public string Indentation { get; private set; }
        public int Indent { set; get; } = 0;

        public AstNode()
        {
            Indentation = string.Empty;
        }

        public void SetIndent(string indent)
        {
            Indentation = indent;
        }

        public virtual void Generate()
        {
            if (string.IsNullOrEmpty(Indentation))
            {
                Indentation = new string(' ', (Indent * 4));
            }
        }

        public virtual List<byte> ByteCode()
        {
            List<byte> OpCodes = new List<byte>();
            return OpCodes;
        }

        public void VerifySeparator(List<Token> tokens)
        {
            var lastToken = tokens.Last();

            if (!(lastToken is StatementSperatorToken && lastToken.Content == ";"))
            {
                throw new Exception($"Error: Se esperaba un ';' al final de la declaración de la cadena. " +
                                    $"Línea: {lastToken.Line}, Columna: {lastToken.Column}");
            }
        }

        public void PrintValues(List<Token> tokens)
        {
            List<string> values = new List<string>();

            foreach (Token token in tokens)
            {
                values.Add(ColorParser.GetTokenColor(token));
            }

            ColorParser.WriteLine($"{string.Join(" ", values)}");
        }
    }
}
