using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using SimpleC.Utils;
using SimpleC.VM;

namespace SimpleC.Types.AstNodes
{
    public class MethodNode : StatementSequenceNode
    {
        public VariableType Type { get; }
        public string Value { get; }
        public List<Token> Parameters { get; } = new List<Token>();
        public string Separator { get; }

        public MethodNode(VariableType type, string name, List<Token> tokens)
        {
            var token = tokens.GetEnumerator();
            token.MoveNext();

            NameAst = $"Metodo: {name}";
            Type = type;
            Value = name;

            if (token.Current?.Content != "(")
            {
                throw new Exception($"Se esperaba un '(': en la línea {token.Current.Line}, posición {token.Current.Column}");
            }

            // Leer si tiene parámetros
            while (token.MoveNext() && token.Current?.Content != ")")
            {
                Parameters.Add(token.Current);
            }

            if (token.Current != null && token.Current?.Content != ")")
            {
                throw new Exception($"Se esperaba un ')': en la línea {token.Current.Line}, posición {token.Current.Column}");
            }

            token.MoveNext();

            if (token.Current != null && token.Current.Content == ";")
            {
                Separator = token.Current.Content;
            }

            // Procesar parámetros como variables locales dentro del bloque
            for (int i = 0; i < Parameters.Count; i++)  // Avanzar de uno en uno
            {
                var typeToken = Parameters[i]; // El tipo (KeywordToken)

                // Verificar si el tipo es válido (KeywordToken)
                if (typeToken is KeywordToken keywordToken)
                {
                    if (!keywordToken.IsTypeKeyword)
                    {
                        throw new Exception($"Error keyword `{typeToken.Content}` no válido. Línea: {typeToken.Line}, columna: {typeToken.Column}");
                    }

                    // Asegurarnos de que hay un nombre de parámetro después del tipo
                    if (i + 1 < Parameters.Count)
                    {
                        var nameToken = Parameters[i + 1]; // El nombre del parámetro (Token)

                        // Convertir el tipo a VariableType
                        var paramType = keywordToken.ToVariableType();

                        // Crear un nuevo parámetro como variable local
                        var paramVar = new ParameterNode(paramType, nameToken.Content);
                        this.AddStatement(paramVar);  // Agregar la variable al bloque

                        i++;  // Avanzamos para saltar al siguiente parámetro
                    }
                    else
                    {
                        throw new Exception($"Error: se esperaba el nombre del parámetro después de `{typeToken.Content}`. Línea: {typeToken.Line}, columna: {typeToken.Column}");
                    }
                }
                else
                {
                    throw new Exception($"Error: se esperaba un tipo válido para el parámetro, pero se encontró `{typeToken.Content}`. Línea: {typeToken.Line}, columna: {typeToken.Column}");
                }

                // Si hay una coma, continuamos con el siguiente parámetro
                if (i + 1 < Parameters.Count && Parameters[i + 1].Content == ",")
                {
                    i++;  // Saltamos la coma
                }
            }

            // Finaliza el método registrando el nombre
            ParserGlobal.Register(Value, this);
        }

        public override void Generate()
        {
            base.Generate();

            List<string> parameters = new List<string>();

            foreach (var parameter in Parameters)
            {
                parameters.Add(ColorParser.GetTokenColor(parameter));
            }

            ColorParser.WriteLine($"[color=blue]{Type.ToLowerString()}[/color] [color=yellow]{Value}[/color][color=magenta]([/color]{string.Join(" ", parameters)}[color=magenta])[/color]{Separator}");


            // Llamamos al BlockNode para generar código
            foreach (var node in this.SubNodes)
            {
                if (node is BlockNode blockNode)
                {
                    blockNode.Indent = Indent;
                    blockNode.Generate();
                }
            }
        }

        public override List<byte> ByteCode()
        {
            List<byte> byteCode = new List<byte>();

            // Mark the beginning of a function with OpCode.Mark
            byteCode.Add((byte)OpCode.Mark);

            // Add the function name length and bytes
            byte[] methodNameBytes = System.Text.Encoding.UTF8.GetBytes(Value);
            byteCode.Add((byte)methodNameBytes.Length); // Name length
            byteCode.AddRange(methodNameBytes); // Function name

            byteCode.Add((byte)OpCode.Enter);

            // Add return type info
            byteCode.Add((byte)GetConstantTypeFromVariableType(Type));

            // Count and process parameters as before
            int paramCount = 0;
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i] is KeywordToken keywordToken && keywordToken.IsTypeKeyword)
                    paramCount++;
            }
            byteCode.Add((byte)paramCount);

            // Process parameter definitions
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i] is KeywordToken keywordToken && keywordToken.IsTypeKeyword)
                {
                    // Parameter type
                    byteCode.Add((byte)GetConstantTypeFromVariableType(keywordToken.ToVariableType()));
                    // Parameter name (next token)
                    if (i + 1 < Parameters.Count && !(Parameters[i + 1] is KeywordToken))
                    {
                        string paramName = Parameters[i + 1].Content;
                        byte[] paramNameBytes = System.Text.Encoding.UTF8.GetBytes(paramName);
                        byteCode.Add((byte)paramNameBytes.Length);
                        byteCode.AddRange(paramNameBytes);
                        i++; // Skip the parameter name token
                    }
                    // Skip comma if present
                    if (i + 1 < Parameters.Count && Parameters[i + 1].Content == ",")
                        i++;
                }
            }

            // Generate method body code
            foreach (var node in this.SubNodes)
            {
                if (node is BlockNode block)
                {
                    byteCode.AddRange(block.ByteCode());
                }
            }

            byteCode.Add((byte)OpCode.Return);

            return byteCode;
        }


        // Helper method to convert VariableType to ConstantType
        private ConstantType GetConstantTypeFromVariableType(VariableType type)
        {
            return type switch
            {
                VariableType.Int => ConstantType.Integer,
                VariableType.Float => ConstantType.Float,
                VariableType.String => ConstantType.String,
                VariableType.Char => ConstantType.Char,
                VariableType.Bool => ConstantType.Bool,
                VariableType.Void => ConstantType.Void,
                _ => throw new Exception($"Unsupported variable type: {type}")
            };
        }

    }
}
