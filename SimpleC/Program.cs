using SimpleC.Lexing;
using SimpleC.Parsing;
using System.Text;

namespace SimpleC
{
    class Program
    {
        const string VERSION = "1.0.0";
        const string AUTHOR = "Desarrollado por AvalonTM, Scarleth Arroyo";
        const string DESCRIPTION = "Compilador para la clase de Autómatas del Instituto Tecnológico de Ensenada (ITE)";

        static void Main(string[] args)
        {
            PrintHeader();

            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            switch (args[0].ToLower())
            {
                case "compile":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Debe especificar un archivo para compilar.");
                        return;
                    }
                    Compile(args[1]);
                    break;

                case "version":
                    Console.WriteLine($"SimpleC versión {VERSION}\n{AUTHOR}\n{DESCRIPTION}");
                    break;

                case "help":
                    PrintUsage();
                    break;

                default:
                    Console.WriteLine("Comando no reconocido. Use 'simplec help' para ver las opciones.");
                    break;
            }
        }

        static void PrintHeader()
        {
            Console.WriteLine("================================");
            Console.WriteLine("        SimpleC Compiler         ");
            Console.WriteLine("================================");
        }

        static void PrintUsage()
        {
            Console.WriteLine("Uso:");
            Console.WriteLine("  simplec compile <archivo>   Compila el archivo especificado");
            Console.WriteLine("  simplec version             Muestra la versión y autor");
            Console.WriteLine("  simplec help                Muestra esta ayuda");
        }

        static void Compile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: El archivo '{filePath}' no existe.");
                return;
            }

            ColorParser.WriteLine($"Compilando '{filePath}'...");
            string code = File.ReadAllText(filePath, Encoding.UTF8);

            // Lexing
            var lexer = new Tokenizer(code);
            var tokens = lexer.Tokenize();

            ColorParser.WriteLine("\nAnalizando...");

            var parser = new Parser(tokens);
            var ast = parser.ParseToAst();

            ColorParser.WriteLine("\nGenerando...");

            foreach (var node in ast.SubNodes)
            {
                node.Generate();
            }

            ColorParser.WriteLine("\n[color=green]¡El Parser se ha generado correctamente![/color]\n");

        }
    }
}
