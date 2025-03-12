using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using SimpleC.Utils;
using SimpleC.VM;
using System.Diagnostics;
using System.Text;

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

        public override List<byte> ByteCode()
        {
            List<byte> opCodes = new List<byte>();

            // Generar bytecode para cada argumento primero (la VM basada en pila necesita los argumentos en la pila antes de la llamada)
            List<Token> arguments = new List<Token>();
            foreach (var arg in Arguments)
            {
                // Omitir paréntesis
                if (arg is OpenBraceToken || arg is CloseBraceToken)
                    continue;

                arguments.Add(arg);
            }

            // Empujar cada argumento a la pila
            foreach (var arg in arguments)
            {
                if (arg is IdentifierToken identifierToken)
                {
                    // Referencia de variable
                    opCodes.Add((byte)OpCode.Load);
                    byte[] varNameBytes = Encoding.UTF8.GetBytes(identifierToken.Content);
                    opCodes.Add((byte)varNameBytes.Length);
                    opCodes.AddRange(varNameBytes);
                }
                else if (arg is NumberLiteralToken numberToken)
                {
                    // Constante entera
                    opCodes.Add((byte)OpCode.LoadC);
                    opCodes.Add((byte)ConstantType.Integer); // Indicador de tipo de constante faltante
                    opCodes.AddRange(BitConverter.GetBytes((int)numberToken.Numero));
                }
                else if (arg is FloatLiteralToken floatToken)
                {
                    // Constante flotante
                    opCodes.Add((byte)OpCode.LoadC);
                    opCodes.Add((byte)ConstantType.Float); // Indicador de tipo de constante faltante
                    opCodes.AddRange(BitConverter.GetBytes(floatToken.Numero));
                }
                else if (arg is StringToken stringToken)
                {
                    // Constante de cadena - eliminar las comillas
                    opCodes.Add((byte)OpCode.LoadC);
                    opCodes.Add((byte)ConstantType.String); // Indicador de tipo de constante faltante

                    // Extraer la cadena sin las comillas (suponiendo que las comillas están en las posiciones 0 y última)
                    string content = stringToken.Content;
                    if (content.StartsWith("\"") && content.EndsWith("\""))
                        content = content.Substring(1, content.Length - 2);

                    byte[] stringBytes = Encoding.UTF8.GetBytes(content);
                    opCodes.Add((byte)stringBytes.Length);
                    opCodes.AddRange(stringBytes);
                }
                else if (arg is BoolToken boolToken)
                {
                    // Constante booleana
                    opCodes.Add((byte)OpCode.LoadC);
                    opCodes.Add((byte)ConstantType.Bool); // Indicador de tipo de constante faltante
                    opCodes.Add((byte)(boolToken.Value ? 1 : 0));
                }
                else if (arg is CharLiteralToken charToken)
                {
                    // Constante de carácter - extraer el carácter de las comillas
                    opCodes.Add((byte)OpCode.LoadC);
                    opCodes.Add((byte)ConstantType.Char); // Indicador de tipo de constante faltante

                    // Extraer el carácter sin las comillas (suponiendo que el formato es 'x')
                    char charValue = charToken.Content.Length >= 3 ? charToken.Content[1] : '\0';
                    opCodes.Add((byte)charValue);
                }
                else
                {
                    throw new Exception($"Tipo de argumento no soportado en la llamada al método: {arg.GetType().Name}");
                }
            }

            // Después de que todos los argumentos estén en la pila, añadir el opcode CALL
            opCodes.Add((byte)OpCode.Call);

            // Añadir el nombre del método
            byte[] methodNameBytes = Encoding.UTF8.GetBytes(Value);
            opCodes.Add((byte)methodNameBytes.Length);
            opCodes.AddRange(methodNameBytes);

            // Añadir el número de argumentos
            opCodes.Add((byte)arguments.Count);

            return opCodes;
        }

    }
}
