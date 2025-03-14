﻿using SimpleC.Parsing;
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
            // Si hay tokens que forman una llamada a función
            // (identificador seguido de paréntesis), considerarlos válidos
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i] is IdentifierToken && tokens[i + 1].Content == "(")
                {
                    string funcName = tokens[i].Content;

                    // Funciones nativas
                    if (funcName == "scanf" || funcName == "input")
                        return expectedType == VariableType.String;

                    if (funcName == "printf" || funcName == "print")
                        return expectedType == VariableType.Int;

                    // Otras funciones - asumimos que son válidas para el tipo esperado
                    // En una implementación más rigurosa, deberíamos verificar su tipo de retorno real
                    return true;
                }
            }

            // Validación estándar para expresiones que no son llamadas a funciones
            VariableType? lastType = null;

            foreach (var token in tokens)
            {
                if (token is OperatorToken || token.Content == "(" || token.Content == ")")
                    continue; // Skip operators and parentheses

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

            return lastType == expectedType || lastType == null;
        }

        private VariableType GetTokenVariableType(Token token)
        {
            // Ignore comma tokens entirely
            if (token.Content == ",") return Type; // Use the current variable's type as a fallback

            // Tipos de datos literales
            if (int.TryParse(token.Content, out _)) return VariableType.Int;
            if (float.TryParse(token.Content, out _)) return VariableType.Float;
            if (token.Content.StartsWith("\"") && token.Content.EndsWith("\"")) return VariableType.String;
            if (token.Content.StartsWith("'") && token.Content.EndsWith("'") && token.Content.Length == 3) return VariableType.Char;
            if (token.Content == "true" || token.Content == "false") return VariableType.Bool;

            // Funciones nativas conocidas
            if (token.Content == "scanf" || token.Content == "input")
            {
                return VariableType.String; // scanf siempre retorna string
            }
            if (token.Content == "printf" || token.Content == "print")
            {
                return VariableType.Int; // printf retorna un int (número de caracteres impresos)
            }

            // Si es un identificador, buscar si es una variable o una función
            if (ParserGlobal.Verify(token.Content))
            {
                var node = ParserGlobal.Get(token.Content);

                // Si es una variable, devolver su tipo
                if (node is VariableNode variable)
                    return variable.Type;

                // Si es una función, devolver su tipo de retorno
                if (node is MethodNode method)
                    return method.Type;

                // Si es una llamada a función, determinar el tipo de la función
                if (node is MethodCallNode methodCall)
                    return methodCall.ReturnType;
            }

            // Para soportar llamadas a funciones no registradas previamente
            // Verificar si los siguientes tokens contienen una estructura de llamada a función
            int index = Values.FindIndex(t => t == token);
            if (index >= 0)
            {
                // Buscar si hay un paréntesis abierto después
                bool isFunctionCall = false;
                for (int i = index + 1; i < Values.Count && !isFunctionCall; i++)
                {
                    if (Values[i].Content == "(")
                    {
                        isFunctionCall = true;
                        break;
                    }

                    // Si encontramos algo que no es un espacio o identificador, no es una llamada a función
                    if (Values[i].Content != " " && !(Values[i] is IdentifierToken))
                        break;
                }

                if (isFunctionCall)
                {
                    // Funciones nativas conocidas
                    if (token.Content == "scanf" || token.Content == "input")
                        return VariableType.String;
                    if (token.Content == "printf" || token.Content == "print")
                        return VariableType.Int;

                    // Buscar en funciones registradas su tipo de retorno
                    if (ParserGlobal.Functions.TryGetValue(token.Content, out MethodNode method))
                    {
                        return method.Type;
                    }

                    // Para funciones definidas por el usuario, intentar encontrar su declaración
                    foreach (var entry in ParserGlobal.Functions)
                    {
                        if (entry.Key == token.Content && entry.Value is MethodNode methodNode)
                        {
                            return methodNode.Type;
                        }
                    }

                    // Si no podemos determinar el tipo, asumir que retorna el mismo tipo que la variable
                    // Esto permite asignaciones como: int x = someFunction();
                    return Type;
                }
            }

            // Default to current variable type if type cannot be determined
            return Type;
        }

        private bool AreCompatibleTypes(VariableType type1, VariableType type2, VariableType expectedType)
        {
            // Si los tipos coinciden exactamente, son compatibles
            if (type1 == expectedType && type2 == expectedType)
                return true;

            // Si alguno de los tipos coincide con el esperado, puede ser compatible
            if (type1 == expectedType || type2 == expectedType)
                return true;

            // Casos especiales de compatibilidad
            return (type1, type2, expectedType) switch
            {
                // Compatibilidad numérica
                (VariableType.Int, VariableType.Int, VariableType.Int) => true,
                (VariableType.Float, VariableType.Float, VariableType.Float) => true,
                (VariableType.Int, VariableType.Float, VariableType.Float) => true,
                (VariableType.Float, VariableType.Int, VariableType.Float) => true,

                // Compatibilidad de strings
                (VariableType.String, VariableType.String, VariableType.String) => true,
                (VariableType.String, _, VariableType.String) => true, // Permitir concatenar cualquier cosa con string
                (_, VariableType.String, VariableType.String) => true, // Permitir concatenar string con cualquier cosa

                // Compatibilidad para retornos de funciones
                (_, _, VariableType.Int) => true,    // Permitir asignación a int
                (_, _, VariableType.Float) => true,  // Permitir asignación a float
                (_, _, VariableType.String) => true, // Permitir asignación a string
                (_, _, VariableType.Bool) => true,   // Permitir asignación a bool
                (_, _, VariableType.Char) => true,   // Permitir asignación a char

                _ => false // Otros casos no son compatibles
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

            // Generar código para la expresión usando la lista Values
            byteCode.AddRange(GenerateExpressionByteCode());

            return byteCode;
        }


        private List<byte> GenerateExpressionByteCode()
        {
            List<byte> byteCode = new List<byte>();

            // Casos simples
            if (Values.Count == 0)
            {
                Token defaultToken = CreateDefaultToken();
                byteCode.AddRange(GenerateLoadCode(defaultToken));
                byteCode.AddRange(GenerateStoreCode());
                return byteCode;
            }

            if (Values.Count == 1)
            {
                byteCode.AddRange(GenerateLoadCode(Values[0]));
                byteCode.AddRange(GenerateStoreCode());
                return byteCode;
            }

            // Convertir a notación postfija (RPN) usando Shunting Yard 
            List<Token> output = ConvertToRPN(Values);

            // Generar bytecode para cada token en notación postfija
            foreach (Token token in output)
            {
                if (token is IdentifierToken || token is NumberLiteralToken ||
                    token is FloatLiteralToken || token is StringToken ||
                    token is CharLiteralToken || token is BoolToken)
                {
                    // Cargar valores a la pila
                    byteCode.AddRange(GenerateLoadCode(token));
                }
                else if (token is OperatorToken op)
                {
                    // Aplicar operador
                    switch (op.Content)
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
                    }
                }
            }

            // Store final result
            byteCode.AddRange(GenerateStoreCode());

            return byteCode;
        }

        // Convierte de notación infija a notación postfija (RPN)
        private List<Token> ConvertToRPN(List<Token> infixTokens)
        {
            List<Token> output = new List<Token>();
            Stack<Token> operatorStack = new Stack<Token>();

            foreach (Token token in infixTokens)
            {
                if (token is IdentifierToken || token is NumberLiteralToken ||
                    token is FloatLiteralToken || token is StringToken ||
                    token is CharLiteralToken || token is BoolToken)
                {
                    // Valores directos a la salida
                    output.Add(token);
                }
                else if (token is OpenBraceToken)
                {
                    // Paréntesis de apertura a la pila
                    operatorStack.Push(token);
                }
                else if (token is CloseBraceToken)
                {
                    // Procesar hasta encontrar el paréntesis de apertura correspondiente
                    while (operatorStack.Count > 0 && !(operatorStack.Peek() is OpenBraceToken))
                    {
                        output.Add(operatorStack.Pop());
                    }

                    // Descartar el paréntesis de apertura
                    if (operatorStack.Count > 0 && operatorStack.Peek() is OpenBraceToken)
                    {
                        operatorStack.Pop();
                    }
                }
                else if (token is OperatorToken currentOp)
                {
                    // Para operadores, aplicar reglas de precedencia
                    while (operatorStack.Count > 0 &&
                           operatorStack.Peek() is OperatorToken stackOp &&
                           GetPrecedence(stackOp) >= GetPrecedence(currentOp))
                    {
                        output.Add(operatorStack.Pop());
                    }

                    operatorStack.Push(token);
                }
            }

            // Mover los operadores restantes a la salida
            while (operatorStack.Count > 0)
            {
                output.Add(operatorStack.Pop());
            }

            return output;
        }

        // Obtener precedencia de operadores
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

        // Método auxiliar para generar bytecode que carga un valor
        // Método auxiliar para generar bytecode que carga un valor
        private List<byte> GenerateLoadCode(Token value)
        {
            List<byte> byteCode = new List<byte>();

            // Verificar si es una llamada a función
            if (value is IdentifierToken identToken)
            {
                // Intentar determinar si este identificador es parte de una llamada a función
                int index = Values.IndexOf(value);

                // Verificar si hay un paréntesis abierto después del identificador
                bool isFunctionCall = index >= 0 && index + 1 < Values.Count && Values[index + 1].Content == "(";

                if (isFunctionCall)
                {
                    // Es una llamada a función, generar bytecode para función
                    return GenerateFunctionCallByteCode(identToken, index);
                }

                // Si no es una llamada a función, continuar con el comportamiento normal
                bool isVarGlobal = this.Verify(identToken.Content) || ParserGlobal.Verify(identToken.Content);
                if (isVarGlobal)
                {
                    byteCode.Add((byte)OpCode.LoadGlobal);
                }
                else
                {
                    byteCode.Add((byte)OpCode.Load);
                }

                byte[] nameBytes = Encoding.UTF8.GetBytes(identToken.Content);
                byteCode.Add((byte)nameBytes.Length);
                byteCode.AddRange(nameBytes);
            }
            else if (value is NumberLiteralToken numberToken)
            {
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Integer);
                byteCode.AddRange(BitConverter.GetBytes(numberToken.Numero));
            }
            else if (value is FloatLiteralToken floatToken)
            {
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Float);
                byteCode.AddRange(BitConverter.GetBytes(floatToken.Numero));
            }
            else if (value is StringToken stringToken)
            {
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.String);
                string strValue = stringToken.Content.Substring(1, stringToken.Content.Length - 2);
                byte[] strBytes = Encoding.UTF8.GetBytes(strValue);
                byteCode.Add((byte)strBytes.Length);
                byteCode.AddRange(strBytes);
            }
            else if (value is BoolToken boolToken)
            {
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Bool);
                byteCode.Add((byte)(boolToken.Value ? 1 : 0));
            }
            else if (value is CharLiteralToken charToken)
            {
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Char);
                char charValue = charToken.Content[1];
                byteCode.Add((byte)charValue);
            }

            return byteCode;
        }

        // Nuevo método para generar bytecode para llamadas a funciones
        private List<byte> GenerateFunctionCallByteCode(IdentifierToken functionToken, int startIndex)
        {
            List<byte> byteCode = new List<byte>();
            string functionName = functionToken.Content;

            // Encontrar el paréntesis de cierre correspondiente
            int openParenIndex = startIndex + 1; // Índice del paréntesis de apertura
            int closeParenIndex = FindMatchingCloseBrace(Values, openParenIndex);

            if (closeParenIndex == -1)
            {
                throw new Exception($"No se encontró el paréntesis de cierre para la llamada a la función '{functionName}'");
            }

            // Extraer los argumentos entre paréntesis
            List<List<Token>> arguments = new List<List<Token>>();
            if (closeParenIndex > openParenIndex + 1) // Si hay argumentos
            {
                // Extraer y separar los argumentos por comas
                int currentArgStart = openParenIndex + 1;
                int parenLevel = 0;

                for (int i = currentArgStart; i < closeParenIndex; i++)
                {
                    Token token = Values[i];

                    if (token.Content == "(")
                    {
                        parenLevel++;
                    }
                    else if (token.Content == ")")
                    {
                        parenLevel--;
                    }
                    else if (token.Content == "," && parenLevel == 0)
                    {
                        // Fin de un argumento
                        if (i > currentArgStart)
                        {
                            arguments.Add(Values.GetRange(currentArgStart, i - currentArgStart));
                        }
                        currentArgStart = i + 1; // Inicio del siguiente argumento
                    }
                }

                // Añadir el último argumento
                if (currentArgStart < closeParenIndex)
                {
                    arguments.Add(Values.GetRange(currentArgStart, closeParenIndex - currentArgStart));
                }
            }

            // Generar bytecode para cada argumento (en orden inverso)
            for (int i = arguments.Count - 1; i >= 0; i--)
            {
                var argTokens = arguments[i];

                // Si el argumento es una expresión simple (un token)
                if (argTokens.Count == 1)
                {
                    byteCode.AddRange(GenerateLoadCode(argTokens[0]));
                }
                else
                {
                    // Para expresiones complejas, necesitaríamos generar el código para evaluar la expresión
                    // Esto podría requerir un enfoque similar al del método GenerateExpressionByteCode

                    // Por simplicidad, manejamos solo el caso de string literal para scanf
                    if (functionName == "scanf" && argTokens.Count == 1 && argTokens[0] is StringToken)
                    {
                        byteCode.AddRange(GenerateLoadCode(argTokens[0]));
                    }
                    else
                    {
                        // Si no es un caso simple, lanzar error o usar un parser de expresiones más completo
                        throw new Exception($"Argumento complejo no soportado en llamada a función: {string.Join(" ", argTokens.Select(t => t.Content))}");
                    }
                }
            }

            // Añadir el opcode Call
            byteCode.Add((byte)OpCode.Call);

            // Añadir el nombre de la función
            byte[] nameBytes = Encoding.UTF8.GetBytes(functionName);
            byteCode.Add((byte)nameBytes.Length);
            byteCode.AddRange(nameBytes);

            // Añadir el número de argumentos
            byteCode.Add((byte)arguments.Count);

            return byteCode;
        }

        // Método auxiliar para generar código de almacenamiento
        private List<byte> GenerateStoreCode()
        {
            List<byte> byteCode = new List<byte>();

            if (IsGlobal)
            {
                byteCode.Add((byte)OpCode.StoreGlobal);
            }
            else
            {
                byteCode.Add((byte)OpCode.Store);
            }

            byteCode.Add((byte)ConvertVariableTypeToConstantType(Type));
            byteCode.AddRange(GetName());

            return byteCode;
        }

        // Método auxiliar para crear tokens predeterminados
        private Token CreateDefaultToken()
        {
            switch (Type)
            {
                case VariableType.Int:
                    return new NumberLiteralToken("0", 0, 0);
                case VariableType.Float:
                    return new FloatLiteralToken("0.0", 0, 0);
                case VariableType.Bool:
                    return new BoolToken("false", 0, 0);
                case VariableType.Char:
                    return new CharLiteralToken("'\\0'", 0, 0);
                case VariableType.String:
                    return new StringToken("\"\"", 0, 0);
                default:
                    throw new Exception($"Tipo de variable no soportado: {Type}");
            }
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

        // Versión corregida del método GenerateValueByteCode
        private List<byte> GenerateValueByteCode(Token value)
        {
            List<byte> byteCode = new List<byte>();

            // Primero, cargar el valor en la pila
            if (value is NumberLiteralToken)
            {
                // Literal entero - primero cargamos la constante en la pila
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Integer);
                int intValue = int.Parse(value.Content);
                byteCode.AddRange(BitConverter.GetBytes(intValue));
            }
            else if (value is FloatLiteralToken)
            {
                // Literal float - primero cargamos la constante en la pila
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Float);
                float floatValue = float.Parse(value.Content);
                byteCode.AddRange(BitConverter.GetBytes(floatValue));
            }
            else if (value is StringToken)
            {
                // Literal string - primero cargamos la constante en la pila
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.String);
                string strValue = value.Content.Substring(1, value.Content.Length - 2);
                byte[] strBytes = Encoding.UTF8.GetBytes(strValue);
                byteCode.Add((byte)strBytes.Length);
                byteCode.AddRange(strBytes);
            }
            else if (value is BoolToken)
            {
                // Literal booleano - primero cargamos la constante en la pila
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Bool);
                byteCode.Add((byte)(value.Content == "true" ? 1 : 0));
            }
            else if (value is CharLiteralToken)
            {
                // Literal de carácter - primero cargamos la constante en la pila
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Char);
                char charValue = value.Content[1];
                byteCode.Add((byte)charValue);
            }
            else if (value is IdentifierToken)
            {
                // Si es una variable, cargarla desde su contexto apropiado
                string varName = value.Content;


                if (IsGlobal)
                {
                    byteCode.Add((byte)OpCode.LoadGlobal);
                }
                else
                {
                    byteCode.Add((byte)OpCode.Load);
                }

                // Añadir nombre de la variable
                byte[] nameBytes = Encoding.UTF8.GetBytes(varName);
                byteCode.Add((byte)nameBytes.Length);
                byteCode.AddRange(nameBytes);
            }
            else
            {
                throw new Exception($"Tipo de valor no soportado: {value.GetType().Name} para el valor {value.Content}");
            }

            // Luego del LoadC, ahora añadir la instrucción para almacenar
            if (IsGlobal)
            {
                byteCode.Add((byte)OpCode.StoreGlobal);
            }
            else
            {
                byteCode.Add((byte)OpCode.Store);
            }

            // Añadir el tipo para la instrucción Store
            byteCode.Add((byte)ConvertVariableTypeToConstantType(Type));

            // Añadir el nombre de la variable
            byteCode.AddRange(GetName());

            return byteCode;
        }

        List<byte> GetName()
        {
            List<byte> byteCode = new List<byte>();

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