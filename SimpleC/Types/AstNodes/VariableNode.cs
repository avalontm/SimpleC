using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using SimpleC.Utils;
using SimpleC.VM;
using System.Diagnostics;
using System.Text;

namespace SimpleC.Types.AstNodes
{
    internal class VariableNode : StatementSequenceNode
    {
        public VariableType Type { get; }
        public Token Identifier { get; }
        public List<Token> Operators { get; }
        public List<Token> Values { get; private set; }
        public bool IsGlobal { get; private set; } // Indica si es global

        // Constructor para declaración de variable
        public VariableNode(VariableType type, Token identifier, List<Token> operators, List<Token> tokens)
        {
            this.Register(identifier.Content, type);

            Type = type;
            Identifier = identifier;

            // Determinar si es global o local
            // Una variable es global si estamos en el ámbito global (fuera de cualquier función)
            IsGlobal = ParserGlobal.IsGlobalScope();
            Debug.WriteLine($"IsGlobal: {Identifier.Content} | {IsGlobal}");
            NameAst = $"Variable: {identifier.Content} {string.Join("", operators.Select(x => x.Content))} {string.Join(" ", tokens.Select(x => x.Content))}";
            Values = new List<Token>();

            if (type != VariableType.Int && type != VariableType.Float &&
                type != VariableType.Char && type != VariableType.String &&
                type != VariableType.Bool && type != VariableType.Void)
            {
                throw new Exception($"Expected a valid variable type");
            }

            // Check if tokens end with ';'
            if (tokens.Count > 0 && tokens.Last() is not StatementSperatorToken && tokens.Last().Content != ";")
            {
                throw new Exception($"Syntax error: Expected ';' at the end of variable declaration '{identifier.Content}'");
            }

            // Normal variable processing
            if (operators.Count > 0)
            {
                Operators = operators;

                foreach (var token in tokens)
                {
                    if (token is not StatementSperatorToken)
                    {
                        Values.Add(token);
                    }
                }

                // Validate assigned values match variable type
                ValidateVariableType();
                ParserGlobal.Register(Identifier.Content, this);
                return;
            }
            else
            {
                // Check if this is a function declaration (has parentheses after name)
                if (tokens.Count > 0 && tokens[0].Content == "(")
                {
                    // Find the matching closing parenthesis to extract parameters
                    int parameterEndIndex = FindMatchingParenthesis(tokens);

                    if (parameterEndIndex != -1)
                    {
                        List<Token> parameters = tokens.GetRange(0, parameterEndIndex + 1);

                        // Register as a method instead of a variable
                        var method = new MethodNode(Type, Identifier.Content, parameters);

                        ParserGlobal.Register(Identifier.Content, method);
                        return;
                    }
                }

                // Plain variable declaration without initialization
                Operators = operators;
                ParserGlobal.Register(Identifier.Content, this);
            }
        }

        // Constructor for variable usage (not declaration)
        public VariableNode(Token identifier)
        {
            Identifier = identifier;
            NameAst = $"VariableUsage: {identifier.Content}";
            Values = new List<Token>();
            Operators = new List<Token>();

            // Try to get the type from the global registry
            if (ParserGlobal.Verify(identifier.Content))
            {
                var variable = ParserGlobal.Get(identifier.Content) as VariableNode;
                if (variable != null)
                {
                    Type = variable.Type;
                    IsGlobal = variable.IsGlobal; // Hereda el estado global/local de la declaración
                }
                else
                {
                    throw new Exception($"Cannot use '{identifier.Content}' as it is not a variable");
                }
            }
            else
            {
                throw new Exception($"Undefined variable: '{identifier.Content}'");
            }
        }

        private int FindMatchingParenthesis(List<Token> tokens)
        {
            int depth = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Content == "(")
                {
                    depth++;
                }
                else if (tokens[i].Content == ")")
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1; // No matching closing parenthesis found
        }

        private void ValidateVariableType()
        {
            bool isValid = true;

            if (Values.Count == 1)
            {
                // Validate single value
                isValid = IsValidValue(Values[0], Type);
            }
            else
            {
                // Validate complete expression
                isValid = IsValidExpression(Values, Type);
            }

            if (!isValid)
            {
                throw new Exception($"Type error: Variable '{Identifier.Content}' of type {Type} cannot contain the expression '{string.Join(" ", Values.Select(v => v.Content))}'");
            }
        }

        private bool IsValidValue(Token token, VariableType expectedType)
        {
            return expectedType switch
            {
                VariableType.Int => int.TryParse(token.Content, out _),
                VariableType.Float => float.TryParse(token.Content, out _),
                VariableType.String => token.Content.StartsWith("\"") && token.Content.EndsWith("\""),
                VariableType.Char => token.Content.StartsWith("'") && token.Content.EndsWith("'") && token.Content.Length == 3,
                VariableType.Bool => token.Content == "true" || token.Content == "false",
                _ => false
            };
        }

        private bool IsValidExpression(List<Token> tokens, VariableType expectedType)
        {
            VariableType? lastType = null;

            foreach (var token in tokens)
            {
                if (token is OperatorToken || token is OpenBraceToken && token.Content == "(" || token is CloseBraceToken && token.Content == ")") continue; // Skip operators (+, -, *, /)

                VariableType currentType = GetTokenVariableType(token);

                if (lastType == null)
                {
                    lastType = currentType;
                }
                else
                {
                    // Validate if types match
                    if (!AreCompatibleTypes(lastType.Value, currentType, expectedType))
                    {
                        return false;
                    }
                }
            }

            return lastType == expectedType;
        }

        private VariableType GetTokenVariableType(Token token)
        {
            if (int.TryParse(token.Content, out _)) return VariableType.Int;
            if (float.TryParse(token.Content, out _)) return VariableType.Float;
            if (token.Content.StartsWith("\"") && token.Content.EndsWith("\"")) return VariableType.String;
            if (token.Content.StartsWith("'") && token.Content.EndsWith("'") && token.Content.Length == 3) return VariableType.Char;
            if (token.Content == "true" || token.Content == "false") return VariableType.Bool;

            // If it's an identifier, search for its type in the global registry
            if (ParserGlobal.Verify(token.Content))
            {
                var variable = ParserGlobal.Get(token.Content) as VariableNode;
                if (variable != null) return variable.Type;
            }

            // Throw exception with line and column information
            throw new Exception($"Error: Cannot determine the type of '{token.Content}' at line {token.Line}, column {token.Column}");
        }

        private bool AreCompatibleTypes(VariableType type1, VariableType type2, VariableType expectedType)
        {
            return (type1, type2, expectedType) switch
            {
                (VariableType.Int, VariableType.Int, VariableType.Int) => true,
                (VariableType.Float, VariableType.Float, VariableType.Float) => true,
                (VariableType.Int, VariableType.Float, VariableType.Float) => true, // Allow int + float
                (VariableType.Float, VariableType.Int, VariableType.Float) => true,
                (VariableType.String, VariableType.String, VariableType.String) => true, // String concatenation
                _ => false
            };
        }

        public override void Generate()
        {
            base.Generate();
            List<string> values = new List<string>();

            foreach (var value in Values)
            {
                values.Add(ColorParser.GetTokenColor(value));
            }

            bool isDeclaration = Type != null;

            if (isDeclaration)
            {
                string scopeIndicator = IsGlobal ? "[color=purple](global)[/color] " : "[color=green](local)[/color] ";
                ColorParser.WriteLine($"{Indentation}{scopeIndicator}[color=blue]{Type.ToLowerString()}[/color] [color=cyan]{Identifier.Content}[/color] [color=white]{string.Join(" ", Operators.Select(x => x.Content))}[/color] {string.Join(" ", values)}");
            }
            else
            {
                ColorParser.WriteLine($"{Indentation}[color=cyan]{Identifier.Content}[/color] {string.Join(" ", values)}");
            }
        }

        public override List<byte> ByteCode()
        {
            List<byte> byteCode = new List<byte>();

            // Registrar la variable
            byteCode.AddRange(GenerateVariableByteCode());

            // Generar código para la expresión usando la lista Values
            byteCode.AddRange(GenerateExpressionByteCode());

            return byteCode;
        }

        // Generar bytecode para expresiones/asignaciones
        private List<byte> GenerateExpressionByteCode()
        {
            List<byte> byteCode = new List<byte>();

            // Si no hay valores explícitos, usar un valor por defecto basado en el tipo
            if (Values.Count == 0)
            {
                // Crear un token temporal con un valor por defecto según el tipo
                Token defaultToken;
                switch (Type)
                {
                    case VariableType.Int:
                        defaultToken = new NumberLiteralToken("0", 0, 0);
                        break;
                    case VariableType.Float:
                        defaultToken = new FloatLiteralToken("0.0", 0, 0);
                        break;
                    case VariableType.Bool:
                        defaultToken = new BoolToken("false", 0, 0);
                        break;
                    case VariableType.Char:
                        defaultToken = new CharLiteralToken("'\\0'", 0, 0);
                        break;
                    case VariableType.String:
                        defaultToken = new StringToken("\"\"", 0, 0);
                        break;
                    default:
                        throw new Exception($"Tipo de variable no soportado: {Type}");
                }

                byteCode.AddRange(GenerateValueByteCode(defaultToken));
                return byteCode;
            }

            // Caso simple - un solo valor
            if (Values.Count == 1)
            {
                byteCode.AddRange(GenerateValueByteCode(Values[0]));
                return byteCode;
            }

            // Expresión con paréntesis - necesitamos manejarla como una subexpresión
            if (Values.Count > 2 && Values[0] is OpenBraceToken && Values[0].Content == "(")
            {
                // Buscar el paréntesis de cierre correspondiente
                int closeParenIndex = FindMatchingCloseBrace(Values, 0);

                if (closeParenIndex > 0)
                {
                    // Extraer subexpresión dentro de los paréntesis
                    List<Token> subExprTokens = new List<Token>();
                    for (int i = 1; i < closeParenIndex; i++)
                    {
                        subExprTokens.Add(Values[i]);
                    }

                    // Crear una sublista temporal de operadores para esta subexpresión
                    List<Token> subOperators = new List<Token>();
                    foreach (var token in subExprTokens)
                    {
                        if (token is OperatorToken)
                        {
                            subOperators.Add(token);
                        }
                    }

                    // Generar código para la subexpresión
                    // Esta es una simplificación; para un manejo completo habría que 
                    // implementar un evaluador de expresiones recursivo

                    // Primer valor de la subexpresión
                    if (subExprTokens.Count > 0 && !(subExprTokens[0] is OperatorToken))
                    {
                        byteCode.AddRange(GenerateValueByteCode(subExprTokens[0]));
                    }

                    // Operaciones en la subexpresión
                    int opCount = 0;
                    for (int i = 1; i < subExprTokens.Count; i++)
                    {
                        if (subExprTokens[i] is OperatorToken)
                        {
                            continue;  // El operador se procesa con el siguiente valor
                        }

                        // Generar valor
                        byteCode.AddRange(GenerateValueByteCode(subExprTokens[i]));

                        // Aplicar operador anterior si existe
                        if (opCount < subOperators.Count)
                        {
                            switch (subOperators[opCount].Content)
                            {
                                case "+":
                                    byteCode.Add((byte)OpCode.Add);
                                    break;
                                case "-":
                                    byteCode.Add((byte)OpCode.Sub);
                                    break;
                                case "*":
                                    byteCode.Add((byte)OpCode.Mul);
                                    break;
                                case "/":
                                    byteCode.Add((byte)OpCode.Div);
                                    break;
                                default:
                                    throw new Exception($"Operador no soportado: {subOperators[opCount].Content}");
                            }
                            opCount++;
                        }
                    }

                    // Si hay más contenido después del paréntesis de cierre, procesarlo
                    if (closeParenIndex < Values.Count - 1)
                    {
                        // Implementar manejo de operaciones adicionales después del paréntesis
                        // Para simplificar, este código no maneja casos complejos como (5+5)*2
                    }

                    return byteCode;
                }
            }

            // Expresión normal (sin paréntesis al inicio)
            // Primer valor
            byteCode.AddRange(GenerateValueByteCode(Values[0]));

            // Procesar operadores y valores subsiguientes
            int operatorIndex = 0;
            for (int i = 1; i < Values.Count; i++)
            {
                // Saltarse operadores, se procesan junto con el siguiente valor
                if (Values[i] is OperatorToken)
                {
                    continue;
                }

                // Generar código para el valor actual
                byteCode.AddRange(GenerateValueByteCode(Values[i]));

                // Aplicar el operador anterior si existe
                if (operatorIndex < i && operatorIndex < Values.Count && Values[operatorIndex] is OperatorToken)
                {
                    switch (Values[operatorIndex].Content)
                    {
                        case "+":
                            byteCode.Add((byte)OpCode.Add);
                            break;
                        case "-":
                            byteCode.Add((byte)OpCode.Sub);
                            break;
                        case "*":
                            byteCode.Add((byte)OpCode.Mul);
                            break;
                        case "/":
                            byteCode.Add((byte)OpCode.Div);
                            break;
                        default:
                            throw new Exception($"Operador no soportado: {Values[operatorIndex].Content}");
                    }

                    // Buscar el siguiente operador
                    operatorIndex = i + 1;
                    while (operatorIndex < Values.Count && !(Values[operatorIndex] is OperatorToken))
                    {
                        operatorIndex++;
                    }
                }
            }

            return byteCode;
        }

        // Método auxiliar para encontrar el paréntesis de cierre correspondiente
        private int FindMatchingCloseBrace(List<Token> tokens, int openBraceIndex)
        {
            int depth = 1;

            for (int i = openBraceIndex + 1; i < tokens.Count; i++)
            {
                if (tokens[i] is OpenBraceToken && tokens[i].Content == "(")
                {
                    depth++;
                }
                else if (tokens[i] is CloseBraceToken && tokens[i].Content == ")")
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1; // No se encontró paréntesis de cierre correspondiente
        }

        private List<byte> GenerateValueByteCode(Token value)
        {
            List<byte> byteCode = new List<byte>();


            // Es un valor literal
            byteCode.Add((byte)OpCode.Store);

            if (value is NumberLiteralToken)
            {
                // Literal entero
                byteCode.Add((byte)ConstantType.Integer);
                int intValue = int.Parse(value.Content);
                byteCode.AddRange(BitConverter.GetBytes(intValue));
            }
            else if (value is FloatLiteralToken)
            {
                // Literal float
                byteCode.Add((byte)ConstantType.Float);
                float floatValue = float.Parse(value.Content);
                byteCode.AddRange(BitConverter.GetBytes(floatValue));
            }
            else if (value is StringToken)
            {
                // Literal string - eliminar las comillas
                byteCode.Add((byte)ConstantType.String);
                string strValue = value.Content.Substring(1, value.Content.Length - 2);
                byte[] strBytes = Encoding.UTF8.GetBytes(strValue);
                byteCode.Add((byte)strBytes.Length);  // Primero almacenar la longitud
                byteCode.AddRange(strBytes);          // Luego los datos de la cadena

            }
            else if (value is BoolToken)
            {
                // Literal booleano
                byteCode.Add((byte)ConstantType.Bool);
                byteCode.Add((byte)(value.Content == "true" ? 1 : 0));
            }
            else if (value is CharLiteralToken)
            {
                // Literal de carácter - extraer el char entre comillas
                byteCode.Add((byte)ConstantType.Char);
                char charValue = value.Content[1];
                byteCode.Add((byte)charValue);
            }
            else
            {
                throw new Exception($"Tipo de valor no soportado: {value.GetType().Name} para el valor {value.Content}");
            }

            return byteCode;
        }

        // Generar bytecode para variables globales
        private List<byte> GenerateVariableByteCode()
        {
            List<byte> byteCode = new List<byte>();

            byteCode.Add((byte)OpCode.Load);

            // Tipo de variable
            byteCode.Add((byte)ConvertVariableTypeToConstantType(Type));

            // Nombre de la variable como bytes
            byte[] nameBytes = Encoding.UTF8.GetBytes(Identifier.Content);
            byteCode.Add((byte)nameBytes.Length); 
            byteCode.AddRange(nameBytes); 

            return byteCode;
        }


        // Convertir VariableType a ConstantType
        private ConstantType ConvertVariableTypeToConstantType(VariableType type)
        {
            return type switch
            {
                VariableType.Int => ConstantType.Integer,
                VariableType.Float => ConstantType.Float,
                VariableType.String => ConstantType.String,
                VariableType.Char => ConstantType.Char,
                VariableType.Bool => ConstantType.Bool,
                VariableType.Void => ConstantType.Void,
                _ => throw new Exception($"Tipo de variable no soportado: {type}")
            };
        }
    }
}