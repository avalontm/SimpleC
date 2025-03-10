using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    public class MethodCallNode : StatementSequenceNode
    {
        public VariableType ReturnType { get; }
        public string Value { get; private set; }
        public List<Token> Arguments { get; }

        public MethodCallNode(VariableType returnType, string name, List<Token> arguments) : base()
        {
            NameAst = $"Llamada de metodo: {name}";
            ReturnType = returnType;
            Value = name;
            Arguments = arguments;

            Debug.WriteLine($"{Indentation}{Value} {string.Join(" ", arguments.Select(x => x.Content))}");

            CheckArgumentsInGlobals();
        }

        // Método para verificar si los argumentos existen en las variables globales
        private void CheckArgumentsInGlobals()
        {
            foreach (var arg in Arguments)
            {
                if (arg is IdentifierToken identifierToken)
                {

                    if (!this.Verify(identifierToken.Content) && !ParserGlobal.Verify(identifierToken.Content)) // Verifica si el nombre del argumento está en los globales
                    {
                        throw new Exception($"{Indentation}Argumento '{identifierToken.Content}' no encontrado: " +
                                            $"(Línea: {identifierToken.Line}, Columna: {identifierToken.Column})");
                    }
                }
            }
        }


        public override void Generate()
        {
            base.Generate();
            List<string> arguments = new List<string>();

            foreach (var arg in Arguments)
            {
                arguments.Add(ColorParser.GetTokenColor(arg));
            }
            ColorParser.WriteLine($"{Indentation}[color=yellow]{Value}[/color] {string.Join(" ", arguments)}");
        }
    }
}
