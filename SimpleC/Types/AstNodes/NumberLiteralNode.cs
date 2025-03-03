using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    internal class NumberLiteralNode : StatementSequenceNode
    {
        public int Value { get; }

        public NumberLiteralNode(int number) 
        {
            Value = number;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Entero: {Value}");
            Console.ResetColor();
        }
    }
}
