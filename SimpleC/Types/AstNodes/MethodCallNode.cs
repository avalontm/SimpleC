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

            // Verificar que los paréntesis estén balanceados
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

            // Obtener argumentos separados por comas (ignorando paréntesis exteriores)
            List<List<Token>> argExpressions = ParseArguments();

            // Generar bytecode para cada argumento
            foreach (var argExpr in argExpressions)
            {
                if (argExpr.Count == 0)
                    continue;

                // Si es una expresión simple (un solo token)
                if (argExpr.Count == 1)
                {
                    GenerateArgumentBytecode(argExpr[0], opCodes);
                }
                else
                {
                    // Es una expresión compleja, usar el parser de expresiones
                    GenerateExpressionBytecode(argExpr, opCodes);
                }
            }

            // Después de que todos los argumentos estén en la pila, añadir el opcode CALL
            opCodes.Add((byte)OpCode.Call);

            // Añadir el nombre del método
            byte[] methodNameBytes = Encoding.UTF8.GetBytes(Value);
            opCodes.Add((byte)methodNameBytes.Length);
            opCodes.AddRange(methodNameBytes);

            // Añadir el número de argumentos
            opCodes.Add((byte)argExpressions.Count);

            return opCodes;
        }

        // Analizar los argumentos separados por comas
        private List<List<Token>> ParseArguments()
        {
            List<List<Token>> result = new List<List<Token>>();

            // Ignorar los paréntesis exteriores
            if (Arguments.Count <= 2) // Solo paréntesis de apertura y cierre, sin contenido
                return result;

            int startIndex = 1; // Empezar después del primer paréntesis
            int endIndex = Arguments.Count - 1; // Terminar antes del último paréntesis

            List<Token> currentArg = new List<Token>();
            int parenthesisLevel = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                Token token = Arguments[i];

                if (token.Content == "(")
                {
                    parenthesisLevel++;
                    currentArg.Add(token);
                }
                else if (token.Content == ")")
                {
                    parenthesisLevel--;
                    currentArg.Add(token);
                }
                else if (token.Content == "," && parenthesisLevel == 0)
                {
                    // Terminar argumento actual en la coma (si no estamos dentro de paréntesis)
                    if (currentArg.Count > 0)
                    {
                        result.Add(currentArg);
                        currentArg = new List<Token>();
                    }
                }
                else
                {
                    currentArg.Add(token);
                }
            }

            // Añadir el último argumento
            if (currentArg.Count > 0)
            {
                result.Add(currentArg);
            }

            return result;
        }

        // Actualizar la función GenerateArgumentBytecode en MethodCallNode
        private void GenerateArgumentBytecode(Token token, List<byte> opCodes)
        {
            if (token is IdentifierToken identifierToken)
            {
                // Para identificadores, usar siempre Load (que priorizará variables locales)
                // La VM primero buscará en contexto local, luego en global
                opCodes.Add((byte)OpCode.Load);

                // Nombre de la variable
                byte[] varNameBytes = Encoding.UTF8.GetBytes(identifierToken.Content);
                opCodes.Add((byte)varNameBytes.Length);
                opCodes.AddRange(varNameBytes);
            }
            else if (token is NumberLiteralToken numberToken)
            {
                // Constante entera
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Integer);
                opCodes.AddRange(BitConverter.GetBytes((int)numberToken.Numero));
            }
            else if (token is FloatLiteralToken floatToken)
            {
                // Constante flotante
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Float);
                opCodes.AddRange(BitConverter.GetBytes(floatToken.Numero));
            }
            else if (token is StringToken stringToken)
            {
                // Constante de cadena
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.String);

                // Extraer la cadena sin las comillas
                string content = stringToken.Content;
                if (content.StartsWith("\"") && content.EndsWith("\""))
                    content = content.Substring(1, content.Length - 2);

                byte[] stringBytes = Encoding.UTF8.GetBytes(content);
                opCodes.Add((byte)stringBytes.Length);
                opCodes.AddRange(stringBytes);
            }
            else if (token is BoolToken boolToken)
            {
                // Constante booleana
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Bool);
                opCodes.Add((byte)(boolToken.Value ? 1 : 0));
            }
            else if (token is CharLiteralToken charToken)
            {
                // Constante de carácter
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Char);
                char charValue = charToken.Content[1]; // Extraer el char de las comillas
                opCodes.Add((byte)charValue);
            }
        }

        // Generar bytecode para una expresión compleja
        private void GenerateExpressionBytecode(List<Token> tokens, List<byte> opCodes)
        {
            // Convertir a notación postfija usando el algoritmo Shunting Yard
            List<Token> postfix = ConvertToPostfix(tokens);

            // Generar bytecode para cada token en notación postfija
            foreach (var token in postfix)
            {
                if (token is IdentifierToken || token is NumberLiteralToken ||
                    token is FloatLiteralToken || token is StringToken ||
                    token is BoolToken || token is CharLiteralToken)
                {
                    GenerateArgumentBytecode(token, opCodes);
                }
                else if (token is OperatorToken opToken)
                {
                    // Generar bytecode para el operador
                    switch (opToken.Content)
                    {
                        case "+":
                            opCodes.Add((byte)OpCode.Add);
                            break;
                        case "-":
                            opCodes.Add((byte)OpCode.Sub);
                            break;
                        case "*":
                            opCodes.Add((byte)OpCode.Mul);
                            break;
                        case "/":
                            opCodes.Add((byte)OpCode.Div);
                            break;
                        default:
                            throw new Exception($"Operador no soportado: {opToken.Content}");
                    }
                }
            }
        }

        // Convertir expresión infija a postfija (notación polaca inversa)
        private List<Token> ConvertToPostfix(List<Token> infix)
        {
            List<Token> output = new List<Token>();
            Stack<Token> operators = new Stack<Token>();

            foreach (var token in infix)
            {
                if (token is IdentifierToken || token is NumberLiteralToken ||
                    token is FloatLiteralToken || token is StringToken ||
                    token is BoolToken || token is CharLiteralToken)
                {
                    // Si es un operando, añadirlo directamente a la salida
                    output.Add(token);
                }
                else if (token.Content == "(")
                {
                    // Si es un paréntesis de apertura, apilar
                    operators.Push(token);
                }
                else if (token.Content == ")")
                {
                    // Si es un paréntesis de cierre, desapilar hasta encontrar el paréntesis de apertura
                    while (operators.Count > 0 && operators.Peek().Content != "(")
                    {
                        output.Add(operators.Pop());
                    }

                    // Descartar el paréntesis de apertura
                    if (operators.Count > 0)
                        operators.Pop();
                }
                else if (token is OperatorToken op)
                {
                    // Si es un operador, aplicar reglas de precedencia
                    while (operators.Count > 0 &&
                           operators.Peek() is OperatorToken topOp &&
                           operators.Peek().Content != "(" &&
                           GetPrecedence(topOp) >= GetPrecedence(op))
                    {
                        output.Add(operators.Pop());
                    }

                    operators.Push(token);
                }
            }

            // Desapilar los operadores restantes
            while (operators.Count > 0)
            {
                output.Add(operators.Pop());
            }

            return output;
        }

        // Obtener la precedencia de un operador
        private int GetPrecedence(OperatorToken op)
        {
            switch (op.Content)
            {
                case "+":
                case "-":
                    return 1;
                case "*":
                case "/":
                    return 2;
                default:
                    return 0;
            }
        }
    }
}