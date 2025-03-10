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
        static bool useDiagram = false;
        static bool logEnabled = false;
        static string diagramPath = "diagrama_ast.png";
        static string logFilePath = "compilation_log.txt";

        static void Main(string[] args)
        {
            Console.ResetColor();

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

                    for (int i = 2; i < args.Length; i++)
                    {
                        if (args[i].ToLower() == "--diagram")
                        {
                            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                            {
                                diagramPath = args[i + 1];
                                i++; // Skip the next argument since it's the path for the diagram
                            }
                            else
                            {
                                // Si no se proporciona una ruta, toma la ruta por defecto
                                diagramPath = "diagrama_ast.png"; // Ruta por defecto para el diagrama
                            }
                            useDiagram = true;
                        }
                        else if (args[i].ToLower() == "--log")
                        {
                            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                            {
                                logFilePath = args[i + 1];
                                i++; // Skip the next argument since it's the path for the log file
                            }
                            else
                            {
                                // Si no se proporciona una ruta, toma la ruta por defecto
                                logFilePath = "compilation_log.txt"; // Ruta por defecto para el log
                            }
                            logEnabled = true;
                        }
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
            Console.WriteLine("  simplec compile <archivo> [--diagram <path>] [--log <path>]   Compila el archivo especificado");
            Console.WriteLine("  simplec version                          Muestra la versión y autor");
            Console.WriteLine("  simplec help                             Muestra esta ayuda");
        }

        static async void Compile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: El archivo '{filePath}' no existe.");
                return;
            }

            ColorParser.WriteLine($"Compilando '{filePath}'...");

            // If log is enabled, log the start of compilation
            if (logEnabled)
            {
                File.AppendAllText(logFilePath, $"Inicio de la compilación: {DateTime.Now}\n");
            }

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

            ColorParser.WriteLine("\n[color=green]¡El Parser se ha generado correctamente![/color]");

            // Log the successful parsing
            if (logEnabled)
            {
                File.AppendAllText(logFilePath, $"Compilación exitosa: {DateTime.Now}\n");
            }

            // Generate diagram if enabled
            if (useDiagram)
            {
                ColorParser.WriteLine($"\n[color=yellow]Generando diagrama...[/color]\n");
                bool status = AstFlowchartGenerator.GenerateAstDiagram(ast, diagramPath);

                if (status)
                {
                    ColorParser.WriteLine($"[color=green]¡Diagrama AST generado en: {diagramPath}![/color]\n");
                    // Log diagram generation
                    if (logEnabled)
                    {
                        File.AppendAllText(logFilePath, $"Diagrama generado: {diagramPath} - {DateTime.Now}\n");
                    }
                }
                else
                {
                    ColorParser.WriteLine("[color=red]Error al generar el diagrama AST.[/color]");
                    if (logEnabled)
                    {
                        File.AppendAllText(logFilePath, $"Error al generar el diagrama: {DateTime.Now}\n");
                    }
                }
            }
        }
    }
}
