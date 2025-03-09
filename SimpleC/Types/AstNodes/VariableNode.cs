using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    internal class VariableNode : StatementSequenceNode
    {
        public VariableType Type { get; }
        public Token Name { get; }
        public List<Token> Operators { get; }
        public List<Token> Values { get; private set; }
        public bool Root { get; }

        public VariableNode(VariableType type, Token name, List<Token> operators, List<Token> tokens, bool root)
        {
            Root = root;
            Values = new List<Token>();

            if (type != VariableType.Int && type != VariableType.Float &&
                type != VariableType.Char && type != VariableType.String &&
                type != VariableType.Bool && type != VariableType.Void)
            {
                throw new Exception($"Se esperaba un tipo de variable válido");
            }

            Type = type;
            Name = name;

            // Procesamiento normal de la variable
            if (operators.Count > 0)
            {
                Debug.WriteLine($"VariableName: {Name.Content}");

                Operators = operators;

                foreach(var token in tokens)
                {
                    if (token is not StatementSperatorToken)
                    {
                        Values.Add(token);
                    }
                }

                ParserGlobal.Register(Name.Content, this);
                ColorParser.WriteLine(this.ToString());
                return;
            }
            else
            {
                // Verificar si esto es una declaración de función (tiene paréntesis después del nombre)
                if (tokens.Count > 0 && tokens[0].Content == "(")
                {
                    Debug.WriteLine($"MethodName: {Name.Content}");
                    // Encontrar el paréntesis de cierre correspondiente para extraer los parámetros
                    int parameterEndIndex = FindMatchingParenthesis(tokens);

                    if (parameterEndIndex != -1)
                    {
                        List<Token> parameters = tokens.GetRange(0, parameterEndIndex + 1);

                        // Registrar como un método en lugar de una variable
                        var method = new MethodNode(Type, Name.Content, parameters);

                        ParserGlobal.Register(Name.Content, method);
                        ColorParser.WriteLine(method.ToString());
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

        public override string ToString()
        {
            List<string> values = new List<string>();

            foreach (var value in Values)
            {
                values.Add(ColorParser.GetTokenColor(value));
            }

            return $"[color=blue]{Type.ToLowerString()}[/color] [color=yellow]{Name.Content}[/color] [color=white]{string.Join(" ", Operators.Select(x=> x.Content))}[/color] {string.Join(" ", values)}";
        }
    }
}
