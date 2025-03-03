using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleC.Types.AstNodes
{
    internal class FloatLiteralNode : StatementSequenceNode
    {
        public float Value { get; }
        public FloatLiteralNode(float value)
        {
            Value = value;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Flotante: {Value}");
            Console.ResetColor();
        }
    }
}
