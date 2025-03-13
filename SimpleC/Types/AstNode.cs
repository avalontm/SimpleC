using SimpleC.Types.AstNodes;
using SimpleC.Types.Tokens;
using SimpleC.Utils;
using System.Diagnostics;

namespace SimpleC.Types
{
    public abstract class AstNode
    {
        public MethodNode? Owner { get; internal set; }
        public string NameAst { get; internal set; }
        public string Indentation { get; private set; }
        public int Indent { set; get; } = 0;

        private List<ParameterNode> Parameters = new List<ParameterNode>();

        private Dictionary<string, VariableType> LocalVariables { get; } = new();

        public void Register(string name, VariableType type)
        {
            if (LocalVariables.ContainsKey(name))
            {
                throw new Exception($"La Variable '{name}' ({type}) ya se ha registrado.");
            }
            LocalVariables[name] = type;
            Debug.WriteLine($"Variable local registrada: {name} ({type})");
        }


        public bool Verify(string name)
        {
            return LocalVariables.ContainsKey(name);
        }

        public VariableType? Get(string key)
        {
            if (!Verify(key))
            {
                return null;
            }
            return LocalVariables[key];
        }

        public VariableType? GetType(string name)
        {
            if (LocalVariables.TryGetValue(name, out var type))
            {
                return type;
            }
            return null;
        }

        public AstNode()
        {
            Indentation = string.Empty;
        }

        public void SetParameters(MethodNode owner, List<ParameterNode> parameters)
        {
            Owner = owner;
            Parameters = parameters;

            foreach (var parameter in parameters)
            {
                this.Register(parameter.Value, parameter.Type);
            }
        }

        public List<ParameterNode> GetParameters()
        {
            return Parameters;
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

        public void SetOwner(MethodNode? owner)
        {
            this.Owner = owner;
        }
    }
}
