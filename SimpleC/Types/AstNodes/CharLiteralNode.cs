using System.Xml.Linq;

namespace SimpleC.Types.AstNodes
{
    internal class CharLiteralNode : StatementSequenceNode
    {
        public char Value { get; }

        public CharLiteralNode(char value)
        {
            Value = value;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Char: '{Value}'");
            Console.ResetColor();
        }
    }
}
