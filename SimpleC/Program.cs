using SimpleC.Lexing;
using SimpleC.Parsing;
using SimpleC.Utils;
using SimpleC.VM;
using System.Diagnostics;
using System.Text;

namespace SimpleC
{
    class Program
    {
        const string VERSION = "1.0.5";
        const string AUTHOR1 = "Desarrollado por los estudiantes:";
        const string AUTHOR2 = "*Jaime Raul Mendez Lopez (23760194)";
        const string AUTHOR3 = "*Scarleth Yoceleth Arroyo Dominguez (23760193)";
        const string DESCRIPTION = "Compilador para la clase de Autómatas del Instituto Tecnológico de Ensenada (ITE)";
        static bool useDiagram = false;
        static bool logEnabled = false;
        static string diagramPath = "diagrama_ast.png";
        static int frameWidth;

        static void Main(string[] args)
        {
            Console.ResetColor();

            try
            {
                frameWidth = Math.Max(65, Console.WindowWidth - 5);
            }
            catch
            {
                frameWidth = 65;
            }

            if (args.Length == 0)
            {
                PrintHeader();
                PrintUsage();
                return;
            }

            string firstArg = args[0].ToLower();

            if (firstArg.EndsWith(".c")) // Si el primer argumento es un archivo .c, lo interpretamos como compilación
            {
                string filePath = args[0];

                for (int i = 1; i < args.Length; i++) // Empezamos en 1 porque el índice 0 es el archivo
                {
                    string arg = args[i].ToLower();

                    switch (arg)
                    {
                        case "--diagram":
                            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                            {
                                diagramPath = args[i + 1];
                                i++;
                            }
                            else
                            {
                                diagramPath = "diagrama_ast.png";
                            }
                            useDiagram = true;
                            break;

                        case "--log":
                            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                            {
                                i++;
                            }
                            else
                            {

                            }
                            logEnabled = true;
                            break;

                        case "--translate":
                            ParserGlobal.IsTranslate = true;
                            break;

                        default:
                            ColorParser.WriteLine($"[color=red]Error: Opción desconocida '{args[i]}'[/color]");
                            return;
                    }
                }

                // Compilamos el archivo especificado
                Compile(filePath);
            }
            else
            {
                PrintHeader();

                switch (firstArg)
                {
                    case "version":
                        PrintVersionInfo();
                        break;

                    case "help":
                        PrintUsage();
                        break;

                    default:
                        ColorParser.WriteLine("[color=red]Comando no reconocido. Use 'simplec help' para ver las opciones.[/color]");
                        break;
                }
            }
        }


        static string CreateHorizontalLine(char leftChar, char middleChar, char rightChar)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(leftChar);
            for (int i = 0; i < frameWidth - 2; i++)
            {
                sb.Append(middleChar);
            }
            sb.Append(rightChar);
            return sb.ToString();
        }

        static string CreateContentLine(string content, char borderChar = '║')
        {
            // Eliminar los códigos de color para calcular la longitud real
            string plainContent = content;
            foreach (var tag in new[] { "[color=green]", "[color=white]", "[color=yellow]", "[color=cyan]", "[color=red]", "[color=blue]", "[/color]" })
            {
                plainContent = plainContent.Replace(tag, "");
            }

            int contentLength = plainContent.Length;
            int totalPadding = frameWidth - 2 - contentLength;
            int leftPadding = totalPadding / 2;
            int rightPadding = totalPadding - leftPadding;

            StringBuilder sb = new StringBuilder();
            sb.Append(borderChar);
            sb.Append(new string(' ', leftPadding));
            sb.Append(content);
            sb.Append(new string(' ', rightPadding));
            sb.Append(borderChar);
            return sb.ToString();
        }
        static string CreateLeftAlignedLine(string content, char borderChar = '║', int leftIndent = 2)
        {
            // Eliminar los códigos de color para calcular la longitud real
            string plainContent = content;
            foreach (var tag in new[] { "[color=green]", "[color=white]", "[color=yellow]", "[color=cyan]", "[color=red]", "[color=blue]", "[/color]" })
            {
                plainContent = plainContent.Replace(tag, "");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(borderChar);
            sb.Append(new string(' ', leftIndent));
            sb.Append(content);

            // Calcular el espacio restante para alinear correctamente el borde derecho
            int rightSpaces = frameWidth - 2 - leftIndent - plainContent.Length;
            if (rightSpaces > 0)
            {
                sb.Append(new string(' ', rightSpaces));
            }

            sb.Append(borderChar);
            return sb.ToString();
        }

        static void PrintHeader()
        {
            Console.WriteLine();
            ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╔', '═', '╗')}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateContentLine("")}[/color]");

            // Para el logo, verificamos si tenemos suficiente espacio
            if (frameWidth >= 50)
            {
                ColorParser.WriteLine($"[color=cyan]║[/color]  [color=yellow]███████╗[/color][color=white]██╗███╗   ███╗██████╗ ██╗     ███████╗[/color]{new string(' ', frameWidth - 50)}[color=cyan]║[/color]");
                ColorParser.WriteLine($"[color=cyan]║[/color]  [color=yellow]██╔════╝[/color][color=white]██║████╗ ████║██╔══██╗██║     ██╔════╝[/color]{new string(' ', frameWidth - 50)}[color=cyan]║[/color]");
                ColorParser.WriteLine($"[color=cyan]║[/color]  [color=yellow]███████╗[/color][color=white]██║██╔████╔██║██████╔╝██║     █████╗  [/color]{new string(' ', frameWidth - 50)}[color=cyan]║[/color]");
                ColorParser.WriteLine($"[color=cyan]║[/color]  [color=yellow]╚════██║[/color][color=white]██║██║╚██╔╝██║██╔═══╝ ██║     ██╔══╝  [/color]{new string(' ', frameWidth - 50)}[color=cyan]║[/color]");
                ColorParser.WriteLine($"[color=cyan]║[/color]  [color=yellow]███████║[/color][color=white]██║██║ ╚═╝ ██║██║     ███████╗███████╗[/color]{new string(' ', frameWidth - 50)}[color=cyan]║[/color]");
                ColorParser.WriteLine($"[color=cyan]║[/color]  [color=yellow]╚══════╝[/color][color=white]╚═╝╚═╝     ╚═╝╚═╝     ╚══════╝╚══════╝[/color]{new string(' ', frameWidth - 50)}[color=cyan]║[/color]");
            }
            else
            {
                // Si la consola es muy estrecha, mostramos solo un título simple
                ColorParser.WriteLine($"[color=cyan]{CreateContentLine("[color=white]SimpleC[/color]")}[/color]");
            }

            ColorParser.WriteLine($"[color=cyan]{CreateContentLine("")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateContentLine("[color=green]C O M P I L E R  v" + VERSION + "[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╚', '═', '╝')}[/color]");
            Console.WriteLine();
        }

        static void PrintVersionInfo()
        {
            Console.WriteLine();
            ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╔', '═', '╗')}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateContentLine("[color=green]SimpleC Compiler v" + VERSION + "[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╠', '═', '╣')}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine($"[color=yellow]{AUTHOR1}[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine($"[color=white]{AUTHOR2}[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine($"[color=white]{AUTHOR3}[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateContentLine("")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine($"[color=white]{DESCRIPTION}[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╚', '═', '╝')}[/color]");
            Console.WriteLine();
        }

        static void PrintUsage()
        {
            Console.WriteLine();
            ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╔', '═', '╗')}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateContentLine("[color=green]INSTRUCCIONES[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╠', '═', '╣')}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine("[color=white]Uso:[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine("[color=yellow]simplec <archivo> [--diagram <path>] [--log <path>][/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine("[color=white]    Compila el archivo especificado[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateContentLine("")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine("[color=yellow]simplec version[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine("[color=white]    Muestra la versión y autor[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateContentLine("")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine("[color=yellow]simplec help[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateLeftAlignedLine("[color=white]    Muestra esta ayuda[/color]")}[/color]");
            ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╚', '═', '╝')}[/color]");
            Console.WriteLine();
        }

        static async void Compile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                ColorParser.WriteLine($"[color=red]Error: El archivo '{filePath}' no existe.[/color]");
                return;
            }

            if (logEnabled)
            {
                // Determinar la longitud real sin tags de color
                string compilingText = $"Compilando: '{filePath}'";
                string dateText = $"Fecha: {DateTime.Now}";

                // Usar formato consistente para el mensaje de compilación
                ColorParser.WriteLine($"[color=yellow]{CreateHorizontalLine('┌', '─', '┐').Replace("═", "─")}[/color]");
                ColorParser.WriteLine($"[color=yellow]│[/color] [color=white]Compilando:[/color] [color=green]'{filePath}'[/color]{new string(' ', frameWidth - compilingText.Length - 3)}[color=yellow]│[/color]");
                ColorParser.WriteLine($"[color=yellow]│[/color] [color=white]Fecha:[/color] [color=green]{DateTime.Now}[/color]{new string(' ', frameWidth - dateText.Length - 3)}[color=yellow]│[/color]");
                ColorParser.WriteLine($"[color=yellow]{CreateHorizontalLine('└', '─', '┘').Replace("═", "─")}[/color]");

            }

            string code = File.ReadAllText(filePath, Encoding.UTF8);

            // Lexing
            if (logEnabled)
            {
                ColorParser.WriteLine("\n[color=yellow]▶ FASE 1: ANÁLISIS LÉXICO[/color]");
            }

            var lexer = new Tokenizer(code);
            var tokens = lexer.Tokenize();

            if (logEnabled)
            {
                ColorParser.WriteLine("[color=green]✓ Análisis léxico completado[/color]");
            }

            // Parsing
            if (logEnabled)
            {
                ColorParser.WriteLine("\n[color=yellow]▶ FASE 2: ANÁLISIS SINTÁCTICO[/color]");
            }

            var parser = new Parser(tokens);
            var ast = parser.ParseToAst();

            if (ast == null)
            {
                return;
            }

            if (logEnabled)
            {
                ColorParser.WriteLine("[color=green]✓ Análisis sintáctico completado[/color]");

                // Codigo generado
                ColorParser.WriteLine($"\n[color=cyan]{CreateHorizontalLine('╔', '═', '╗')}[/color]");
                ColorParser.WriteLine($"[color=cyan]{CreateContentLine("[color=green]C O D I G O [/color]")}[/color]");
                ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╚', '═', '╝')}[/color]");
                Console.WriteLine();

                foreach (var node in ast.SubNodes)
                {
                    node.Generate();
                }

                Console.WriteLine();
                ColorParser.WriteLine("[color=green]✓ Generación de código completada[/color]");

                ColorParser.WriteLine($"\n[color=cyan]{CreateHorizontalLine('╔', '═', '╗')}[/color]");
                ColorParser.WriteLine($"[color=cyan]{CreateContentLine("[color=green]COMPILACIÓN EXITOSA[/color]")}[/color]");
                ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╚', '═', '╝')}[/color]");

                ColorParser.WriteLine($"\n[color=cyan]{CreateHorizontalLine('╔', '═', '╗')}[/color]");
                ColorParser.WriteLine($"[color=cyan]{CreateContentLine("[color=green]E J E C U T A N D O[/color]")}[/color]");
                ColorParser.WriteLine($"[color=cyan]{CreateHorizontalLine('╚', '═', '╝')}[/color]");
            }

            List<byte> OpCodes = new List<byte>();

            try
            {
                foreach (var node in ast.SubNodes)
                {
                    OpCodes.AddRange(node.ByteCode());
                }

                OpCodes.Add((byte)OpCode.Halt);

                VirtualMachine vm = new VirtualMachine(OpCodes);
                if (logEnabled)
                {
                    vm.DebugBytecode();
                }
                vm.Run();
            }
            catch (Exception ex)
            {
                ColorParser.WriteLine($"[color=red]{ex}[/color]");
            }

            // Generamos el diagrama si esta activado
            if (useDiagram)
            {
                ColorParser.WriteLine($"\n[color=yellow]▶ GENERANDO DIAGRAMA AST[/color]");
                bool status = AstFlowchartGenerator.GenerateAstDiagram(ast, diagramPath);

                if (status)
                {
                    ColorParser.WriteLine($"[color=green]✓ Diagrama AST generado en: {diagramPath}[/color]");
                }
                else
                {
                    ColorParser.WriteLine("[color=red]✗ Error al generar el diagrama AST[/color]");
                }
            }

        }
    }
}