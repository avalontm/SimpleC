using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using SimpleC.VM;
using System.Text;

namespace SimpleC.Types.AstNodes
{
    public class VariableReferenceNode : StatementSequenceNode
    {
        public Token Value { get; private set; }
        public VariableType Type { get; private set; }

        public VariableReferenceNode(Token value, VariableType type) : base()
        {
            NameAst = $"Referencia: {value.Content}";
            Type = type;
            Value = value;
        }

        public override List<byte> ByteCode()
        {
            List<byte> byteCode = new List<byte>();

            // 1. Generar bytecode para cargar el valor en la pila
            // Esto dependerá del tipo de token que es Value

            if (Value is IdentifierToken identToken)
            {
                // Si es un identificador, cargamos la variable
                // Determinar si es global o local
                bool isGlobal = ParserGlobal.Verify(identToken.Content);
                if (isGlobal)
                {
                    byteCode.Add((byte)OpCode.LoadGlobal);
                }
                else
                {
                    byteCode.Add((byte)OpCode.Load);
                }

                // Añadir el nombre de la variable
                byte[] nameBytes = Encoding.UTF8.GetBytes(identToken.Content);
                byteCode.Add((byte)nameBytes.Length);
                byteCode.AddRange(nameBytes);
            }
            else if (Value is NumberLiteralToken numToken)
            {
                // Si es un número literal
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Integer);
                byteCode.AddRange(BitConverter.GetBytes(numToken.Numero));
            }
            else if (Value is FloatLiteralToken floatToken)
            {
                // Si es un float literal
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Float);
                byteCode.AddRange(BitConverter.GetBytes(floatToken.Numero));
            }
            else if (Value is StringToken strToken)
            {
                // Si es una cadena literal
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.String);

                // Remover comillas
                string strValue = strToken.Content;
                if (strValue.StartsWith("\"") && strValue.EndsWith("\""))
                    strValue = strValue.Substring(1, strValue.Length - 2);

                byte[] strBytes = Encoding.UTF8.GetBytes(strValue);
                byteCode.Add((byte)strBytes.Length);
                byteCode.AddRange(strBytes);
            }
            else if (Value is BoolToken boolToken)
            {
                // Si es un booleano literal
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Bool);
                byteCode.Add((byte)(boolToken.Value ? 1 : 0));
            }
            else if (Value is CharLiteralToken charToken)
            {
                // Si es un carácter literal
                byteCode.Add((byte)OpCode.LoadC);
                byteCode.Add((byte)ConstantType.Char);

                // Extraer el carácter (removiendo comillas simples)
                char charValue = charToken.Content[1]; // Suponiendo formato 'a'
                byteCode.Add((byte)charValue);
            }

            // 2. Si este nodo es parte de una asignación, el Store se generará en el nodo padre
            // Si necesitas generar una instrucción Store aquí, dependerá del contexto en que
            // se use este nodo y se necesitará información adicional

            return byteCode;
        }
    }
}