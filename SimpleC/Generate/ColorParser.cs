using SimpleC.Types;
using SimpleC.Types.Tokens;

namespace SimpleC
{
    public static class ColorParser
    {
        public static void WriteLine(string input, bool newLine = true)
        {
            int startIndex = 0;
            while (startIndex < input.Length)
            {
                // Buscar el inicio de un color
                int colorStart = input.IndexOf("[color=", startIndex);
                if (colorStart == -1)
                {
                    // Si no hay más colores, imprimir el resto del texto
                    Console.Write(input.Substring(startIndex));
                    break;
                }

                // Imprimir el texto antes del marcador de color
                Console.Write(input.Substring(startIndex, colorStart - startIndex));

                // Buscar el color específico
                int colorEnd = input.IndexOf("]", colorStart);
                if (colorEnd == -1)
                {
                    // Si no hay cierre de marcador de color, salir del ciclo
                    break;
                }

                // Obtener el nombre del color
                string colorName = input.Substring(colorStart + 7, colorEnd - (colorStart + 7));

                // Establecer el color de la consola según el nombre
                SetConsoleColor(colorName);

                // Buscar el cierre de la etiqueta [color]
                int colorClose = input.IndexOf("[/color]", colorEnd);
                if (colorClose == -1)
                {
                    break;
                }

                // Imprimir el texto dentro del marcador de color
                int nextStartIndex = colorClose + 8; // El tamaño de "[/color]" es 8
                Console.Write(input.Substring(colorEnd + 1, colorClose - colorEnd - 1));

                // Restablecer el color al predeterminado
                Console.ResetColor();

                // Continuar con el resto del texto
                startIndex = nextStartIndex;
            }
            if (newLine)
            {
                Console.WriteLine();
            }
        }

        static void SetConsoleColor(string colorName)
        {
            // Establecer el color de la consola basado en el nombre
            switch (colorName.ToLower())
            {
                case "red":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "green":
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case "blue":
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case "yellow":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "cyan":
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case "magenta":
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case "white":
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case "orange":
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
        }

        public static string GetTokenColor(Token token)
        {
            // Usamos el operador 'is' para comparar el tipo del token
            switch (token)
            {
                case KeywordToken keywordToken:
                    return $"[color=blue]{keywordToken.Content}[/color]"; // Colorear como palabra clave (keyword)
                case IdentifierToken identifierToken:
                    return $"[color=cyan]{identifierToken.Content}[/color]"; // Colorear como identificador
                case NumberLiteralToken numberLiteralToken:
                    return $"[color=green]{numberLiteralToken.Content}[/color]"; // Colorear como número
                case FloatLiteralToken floatLiteralToken:
                    return $"[color=green]{floatLiteralToken.Content}[/color]"; // Colorear como número flotante
                case StringToken stringToken:
                    return $"[color=orange]{stringToken.Content}[/color]"; // Colorear como cadena de texto
                case LibraryToken libraryToken:
                    return $"[color=orange]{libraryToken.Content}[/color]"; 
                case OpenBraceToken openbraceToken:
                    return $"[color=magenta]{openbraceToken.Content}[/color]";
                case CloseBraceToken closebraceToken:
                    return $"[color=magenta]{closebraceToken.Content}[/color]";
                case ReturnToken returnToken:
                    return $"[color=magenta]{returnToken.Content}[/color]";
                default:
                    return $"[color=gray]{token.Content}[/color]"; // Si no es reconocido, no colorear
            }
        }

    }
}
