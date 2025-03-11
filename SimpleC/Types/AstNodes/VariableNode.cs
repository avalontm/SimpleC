using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    internal class VariableNode : StatementSequenceNode
    {
        public VariableType Type { get; }
        public Token Identifier { get; }
        public List<Token> Operators { get; }
        public List<Token> Values { get; private set; }

        public VariableNode(VariableType type, Token identifier, List<Token> operators, List<Token> tokens)
        {
            this.Register(identifier.Content, type);

            Type = type;
            Identifier = identifier;
            Debug.WriteLine($"VariableNode: {identifier.Content}");
            NameAst = $"Variable: {identifier.Content} {string.Join("", operators.Select(x => x.Content))} {string.Join(" ", tokens.Select(x => x.Content))}";
            Values = new List<Token>();

            if (type != VariableType.Int && type != VariableType.Float &&
                type != VariableType.Char && type != VariableType.String &&
                type != VariableType.Bool && type != VariableType.Void)
            {
                throw new Exception($"Se esperaba un tipo de variable válido");
            }

            // Verificar que los tokens terminen con ';'
            if (tokens.Count > 0 && tokens.Last() is not StatementSperatorToken && tokens.Last().Content != ";")
            {
                throw new Exception($"Error de sintaxis: Se esperaba ';' al final de la declaración de la variable '{identifier.Content}'");
            }

            // Procesamiento normal de la variable
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

                // Validar que los valores asignados sean del mismo tipo que la variable
                ValidateVariableType();
                ParserGlobal.Register(Identifier.Content, this);
                return;
            }
            else
            {
                // Verificar si esto es una declaración de función (tiene paréntesis después del nombre)
                if (tokens.Count > 0 && tokens[0].Content == "(")
                {
                    // Encontrar el paréntesis de cierre correspondiente para extraer los parámetros
                    int parameterEndIndex = FindMatchingParenthesis(tokens);

                    if (parameterEndIndex != -1)
                    {
                        List<Token> parameters = tokens.GetRange(0, parameterEndIndex + 1);

                        // Registrar como un método en lugar de una variable
                        var method = new MethodNode(Type, Identifier.Content, parameters);

                        ParserGlobal.Register(Identifier.Content, method);
                        return;
                    }
                }
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

            return -1; // No se encontró un paréntesis de cierre correspondiente
        }

        private void ValidateVariableType()
        {
            bool isValid = true;

            if (Values.Count == 1)
            {
                // Validar valor único
                isValid = IsValidValue(Values[0], Type);
            }
            else
            {
                // Validar expresión completa
                isValid = IsValidExpression(Values, Type);
            }

            if (!isValid)
            {
                throw new Exception($"Error de tipo: La variable '{Identifier.Content}' de tipo {Type} no puede contener la expresión '{string.Join(" ", Values.Select(v => v.Content))}'");
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
                if (token is OperatorToken || token is OpenBraceToken && token.Content == "(" || token is CloseBraceToken && token.Content == ")") continue; // Saltar operadores (+, -, *, /)

                VariableType currentType = GetTokenVariableType(token);

                if (lastType == null)
                {
                    lastType = currentType;
                }
                else
                {
                    // Validar si los tipos coinciden
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

            // Si es un identificador, buscar su tipo en el registro global
            if (ParserGlobal.Verify(token.Content))
            {
                var variable = ParserGlobal.Get(token.Content) as VariableNode;
                if (variable != null) return variable.Type;
            }

            // Lanzar una excepción con información de línea y columna
            throw new Exception($"Error: No se puede determinar el tipo de '{token.Content}' en la línea {token.Line}, columna {token.Column}");
        }


        private bool AreCompatibleTypes(VariableType type1, VariableType type2, VariableType expectedType)
        {
            return (type1, type2, expectedType) switch
            {
                (VariableType.Int, VariableType.Int, VariableType.Int) => true,
                (VariableType.Float, VariableType.Float, VariableType.Float) => true,
                (VariableType.Int, VariableType.Float, VariableType.Float) => true, // Permite int + float
                (VariableType.Float, VariableType.Int, VariableType.Float) => true,
                (VariableType.String, VariableType.String, VariableType.String) => true, // Concatenación de strings
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

            ColorParser.WriteLine($"{Indentation}[color=blue]{Type.ToLowerString()}[/color] [color=cyan]{Identifier.Content}[/color] [color=white]{string.Join(" ", Operators.Select(x => x.Content))}[/color] {string.Join(" ", values)}");

        }
    }
}
