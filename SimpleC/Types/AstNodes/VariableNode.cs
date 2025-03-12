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
        public bool IsDeclaration { get; private set; }

        public VariableNode(VariableType type, Token identifier, List<Token> operators, List<Token> tokens)
        {
            this.Register(identifier.Content, type);

            Type = type;
            Identifier = identifier;
            IsDeclaration = true; // By default, this is a declaration
            Debug.WriteLine($"VariableNode: {identifier.Content}");
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
            IsDeclaration = false;
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

            if (IsDeclaration)
            {
                ColorParser.WriteLine($"{Indentation}[color=blue]{Type.ToLowerString()}[/color] [color=cyan]{Identifier.Content}[/color] [color=white]{string.Join(" ", Operators.Select(x => x.Content))}[/color] {string.Join(" ", values)}");
            }
            else
            {
                ColorParser.WriteLine($"{Indentation}[color=cyan]{Identifier.Content}[/color] {string.Join(" ", values)}");
            }
        }

        public override List<byte> ByteCode()
        {
            List<byte> byteCode = new List<byte>();

            if (IsDeclaration)
            {
                // If variable has operators and values, process them to generate opcodes
                if (Operators.Count > 0 && Values.Count > 0)
                {
                    // First load the values to the stack
                    byteCode.AddRange(GenerateExpressionByteCode());

                    // Then store them in the variable
                    byteCode.AddRange(GenerateStoreByteCode());
                }
                else
                {
                    // Just a declaration, push default value onto stack based on type
                    byteCode.AddRange(GenerateDefaultValueByteCode());

                    // Then store it in the variable
                    byteCode.AddRange(GenerateStoreByteCode());
                }
            }
            else
            {
                // Variable usage - load the variable's value onto stack
                byteCode.AddRange(GenerateLoadByteCode());
            }

            return byteCode;
        }

        // Generate bytecode for loading default value based on type
        private List<byte> GenerateDefaultValueByteCode()
        {
            List<byte> byteCode = new List<byte>();

            // Push the appropriate default value for the type
            byteCode.Add((byte)OpCode.LoadC); // Load constant

            switch (Type)
            {
                case VariableType.Int:
                    byteCode.Add((byte)ConstantType.Integer);
                    byteCode.AddRange(BitConverter.GetBytes(0));
                    break;

                case VariableType.Float:
                    byteCode.Add((byte)ConstantType.Float);
                    byteCode.AddRange(BitConverter.GetBytes(0.0f));
                    break;

                case VariableType.Bool:
                    byteCode.Add((byte)ConstantType.Bool);
                    byteCode.Add(0); // false
                    break;

                case VariableType.Char:
                    byteCode.Add((byte)ConstantType.Char);
                    byteCode.Add((byte)'\0');
                    break;

                case VariableType.String:
                    byteCode.Add((byte)ConstantType.String);
                    byteCode.Add(0); // Empty string, length 0
                    break;

                default:
                    throw new Exception($"Unsupported variable type: {Type}");
            }

            return byteCode;
        }

        // Generate bytecode for storing the top of stack in this variable
        private List<byte> GenerateStoreByteCode()
        {
            List<byte> byteCode = new List<byte>();

            // Opcode for Store
            byteCode.Add((byte)OpCode.Store);

            // Variable name as bytes
            byte[] nameBytes = Encoding.UTF8.GetBytes(Identifier.Content);
            byteCode.Add((byte)nameBytes.Length); // Length of name
            byteCode.AddRange(nameBytes); // The name itself

            return byteCode;
        }

        // Generate bytecode for loading this variable onto the stack
        private List<byte> GenerateLoadByteCode()
        {
            List<byte> byteCode = new List<byte>();

            // Opcode for Load
            byteCode.Add((byte)OpCode.Load);

            // Variable name as bytes
            byte[] nameBytes = Encoding.UTF8.GetBytes(Identifier.Content);
            byteCode.Add((byte)nameBytes.Length); // Length of name
            byteCode.AddRange(nameBytes); // The name itself

            return byteCode;
        }

        // Generate bytecode for expressions/assignments
        private List<byte> GenerateExpressionByteCode()
        {
            List<byte> byteCode = new List<byte>();

            // Simple case - single value
            if (Values.Count == 1)
            {
                byteCode.AddRange(GenerateValueByteCode(Values[0]));
                return byteCode;
            }

            // More complex expression
            // For now, we'll implement just simple expressions (no precedence)
            // In a real compiler, you'd build an expression tree and evaluate it

            // First value
            byteCode.AddRange(GenerateValueByteCode(Values[0]));

            // Process operators and values
            for (int i = 0; i < Operators.Count && i < Values.Count - 1; i++)
            {
                // Next value
                byteCode.AddRange(GenerateValueByteCode(Values[i + 1]));

                // Apply operator
                switch (Operators[i].Content)
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
                    // Add more operators as needed
                    default:
                        throw new Exception($"Unsupported operator: {Operators[i].Content}");
                }
            }

            return byteCode;
        }

        // Generate bytecode for a value (e.g., a literal or result of an operation)
        private List<byte> GenerateValueByteCode(Token value)
        {
            List<byte> byteCode = new List<byte>();

            // Check if this is a variable reference
            if (ParserGlobal.Verify(value.Content) && !(value is NumberLiteralToken || value is FloatLiteralToken
                || value is StringToken || value is BoolToken || value is CharLiteralToken))
            {
                // Load the variable
                byteCode.Add((byte)OpCode.Load);
                byte[] varNameBytes = Encoding.UTF8.GetBytes(value.Content);
                byteCode.Add((byte)varNameBytes.Length);
                byteCode.AddRange(varNameBytes);
                return byteCode;
            }

            // It's a literal value
            byteCode.Add((byte)OpCode.LoadC); // Load constant

            if (value is NumberLiteralToken)
            {
                // Integer literal
                byteCode.Add((byte)ConstantType.Integer);
                int intValue = int.Parse(value.Content);
                byteCode.AddRange(BitConverter.GetBytes(intValue));
            }
            else if (value is FloatLiteralToken)
            {
                // Float literal
                byteCode.Add((byte)ConstantType.Float);
                float floatValue = float.Parse(value.Content);
                byteCode.AddRange(BitConverter.GetBytes(floatValue));
            }
            else if (value is StringToken)
            {
                // String literal - trim the quotes
                byteCode.Add((byte)ConstantType.String);
                string strValue = value.Content.Substring(1, value.Content.Length - 2);
                byte[] strBytes = Encoding.UTF8.GetBytes(strValue);
                byteCode.Add((byte)strBytes.Length);
                byteCode.AddRange(strBytes);
            }
            else if (value is BoolToken)
            {
                // Boolean literal
                byteCode.Add((byte)ConstantType.Bool);
                byteCode.Add((byte)(value.Content == "true" ? 1 : 0));
            }
            else if (value is CharLiteralToken)
            {
                // Character literal - extract the char from between quotes
                byteCode.Add((byte)ConstantType.Char);
                char charValue = value.Content[1];
                byteCode.Add((byte)charValue);
            }
            else
            {
                throw new Exception($"Unsupported value type: {value.GetType().Name} for value {value.Content}");
            }

            return byteCode;
        }
    }
}