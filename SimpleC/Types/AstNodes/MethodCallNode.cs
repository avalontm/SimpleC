using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    public class MethodCallNode : StatementSequenceNode
    {
        public VariableType ReturnType { get; }
        public string Value { get; private set; }
        public List<Token> Arguments { get; private set; }
        Token separator;
        public MethodCallNode(VariableType returnType, string name, List<Token> arguments) : base()
        {
            NameAst = $"Llamada de metodo: {name}";
            ReturnType = returnType;
            Value = name;
            Arguments = arguments;

            Debug.WriteLine($"{Indentation}{Value} {string.Join(" ", arguments.Select(x => x.Content))}");

            // Verificar si la llamada termina con un punto y coma
            CheckEndingWithSemicolon();
            // Verificar la sintaxis de los argumentos y verificar si están bien delimitados
            CheckArgumentsSyntax();
            // Verificar si los argumentos existen en las variables globales
            CheckArgumentsInGlobals();
        }

        // Verificar que los argumentos estén correctamente delimitados por paréntesis
        private void CheckArgumentsSyntax()
        {
            
            if (Arguments.Count == 0 || Arguments.First().Content != "(" || Arguments.Last().Content != ")")
            {
                throw new Exception($"{Indentation}Error de sintaxis en la llamada al método '{Value}': " +
                                    $"Los argumentos deben estar entre paréntesis. (Línea: {Arguments.First().Line}, Columna: {Arguments.First().Column})");
            }
        
            // Verificar que los paréntesis estén balanceados y que no haya contenido antes del primer paréntesis o después del último
            int openParens = 0;
            foreach (var token in Arguments)
            {
                if (token.Content == "(")
                {
                    openParens++;
                }
                else if (token.Content == ")")
                {
                    openParens--;
                }

                // Si los paréntesis no están balanceados
                if (openParens < 0)
                {
                    throw new Exception($"{Indentation}Error de sintaxis en la llamada al método '{Value}': " +
                                        $"Paréntesis cerrados de manera incorrecta. (Línea: {token.Line}, Columna: {token.Column})");
                }
            }

            // Verificar si el número de paréntesis abiertos y cerrados es el mismo
            if (openParens != 0)
            {
                throw new Exception($"{Indentation}Error de sintaxis en la llamada al método '{Value}': " +
                                    $"Paréntesis no balanceados. (Línea: {Arguments.Last().Line}, Columna: {Arguments.Last().Column})");
            }
        }

        private void CheckEndingWithSemicolon()
        {
            // Verifica si la lista Arguments no está vacía antes de intentar acceder a su último elemento
            if (Arguments.Count == 0)
            {
                throw new Exception($"{Indentation}Error de sintaxis en la llamada al método '{Value}': " +
                                     "No se encontraron argumentos. (Línea: 0, Columna: 0)");
            }

            // Verificar si el último elemento es un StatementSperatorToken con el contenido ";"
            var lastToken = Arguments.LastOrDefault();
            if (lastToken != null && lastToken is StatementSperatorToken statement && statement.Content == ";")
            {
                separator = lastToken;
                Arguments.Remove(lastToken); 
            }
            else if (lastToken != null)
            {
                // Si no es un punto y coma, lanzar un error con el último argumento
                throw new Exception($"{Indentation}Error de sintaxis en la llamada al método '{Value}': " +
                                     $"Se esperaba (;) al final de la llamada. (Línea: {lastToken.Line}, Columna: {lastToken.Column})");
            }
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
            ColorParser.WriteLine($"{Indentation}[color=yellow]{Value}[/color] {string.Join(" ", arguments)} {separator.Content}");
        }
    }
}
