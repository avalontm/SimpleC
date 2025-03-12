using SimpleC.Types.Tokens;
using SimpleC.VM;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    internal class StringNode : StatementSequenceNode
    {
        public List<Token> Values;

        public StringNode(List<Token> values)
        {
            NameAst = "Cadena de texto";
            Values = values;

            VerifySeparator(values);
        }

        public override void Generate()
        {
            base.Generate();
            PrintValues(Values);
        }

        public override List<byte> ByteCode()
        {
            List<byte> OpCodes = new List<byte>();

            Debug.WriteLine($"string: {Indent}");
            foreach (var token in Values)
            {
                // Solo procesamos si el token es un StringToken
                if (token is StringToken stringToken)
                {
                    byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(stringToken.Content);

                    OpCodes.Add((byte)OpCode.LoadS); // Opcode para LOAD_STRING
                    OpCodes.Add((byte)stringBytes.Length); // Longitud de la cadena
                    OpCodes.AddRange(stringBytes); // Cuerpo de la cadena
                }
            }

            return OpCodes;
        }
    }
}
