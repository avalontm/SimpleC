using SimpleC.Parsing;
using SimpleC.Types;
using SimpleC.Types.Tokens;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleC
{
    public static class ColorParser
    {
        /// <summary>
        /// Writes a string to the console with color formatting.
        /// Supports color tags in the format [color=name]text[/color] and nested color tags.
        /// </summary>
        /// <param name="input">The formatted string to write</param>
        /// <param name="newLine">Whether to append a newline after writing</param>
        public static void WriteLine(string input, bool newLine = true)
        {
            if (string.IsNullOrEmpty(input))
            {
                if (newLine) Console.WriteLine();
                return;
            }

            // Save original color to restore at the end
            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                ProcessColorTags(input);
            }
            finally
            {
                // Ensure color is reset even if an exception occurs
                Console.ForegroundColor = originalColor;
            }

            if (newLine)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Processes color tags in the input string using regex for more reliable parsing.
        /// </summary>
        /// <param name="input">The input string to process</param>
        private static void ProcessColorTags(string input)
        {
            int currentIndex = 0;
            Stack<ConsoleColor> colorStack = new Stack<ConsoleColor>();
            colorStack.Push(Console.ForegroundColor); // Save original color

            while (currentIndex < input.Length)
            {
                // Find the next open or close tag
                int openTagStart = input.IndexOf("[color=", currentIndex);
                int closeTagStart = input.IndexOf("[/color]", currentIndex);

                // No more tags, print the rest and exit
                if (openTagStart == -1 && closeTagStart == -1)
                {
                    Console.Write(input.Substring(currentIndex));
                    break;
                }

                // Determine which tag comes first
                bool isOpenTagNext = openTagStart != -1 && (closeTagStart == -1 || openTagStart < closeTagStart);

                if (isOpenTagNext)
                {
                    // Print text before the opening tag
                    if (openTagStart > currentIndex)
                    {
                        Console.Write(input.Substring(currentIndex, openTagStart - currentIndex));
                    }

                    // Find the end of the color specification
                    int colorEnd = input.IndexOf("]", openTagStart);
                    if (colorEnd == -1)
                    {
                        // Malformed tag, print the rest as is
                        Console.Write(input.Substring(openTagStart));
                        break;
                    }

                    // Extract the color name
                    string colorName = input.Substring(openTagStart + 7, colorEnd - (openTagStart + 7));

                    // Set the new color
                    ConsoleColor newColor = ParseColorName(colorName);
                    colorStack.Push(newColor);
                    Console.ForegroundColor = newColor;

                    // Move past this tag
                    currentIndex = colorEnd + 1;
                }
                else // Close tag is next
                {
                    // Print text before the closing tag
                    if (closeTagStart > currentIndex)
                    {
                        Console.Write(input.Substring(currentIndex, closeTagStart - currentIndex));
                    }

                    // Pop the current color to return to the previous one
                    if (colorStack.Count > 1) // Keep at least the original color
                    {
                        colorStack.Pop();
                        Console.ForegroundColor = colorStack.Peek();
                    }

                    // Move past this closing tag
                    currentIndex = closeTagStart + 8; // Length of "[/color]"
                }
            }
        }

        /// <summary>
        /// Parses a color name into a ConsoleColor
        /// </summary>
        /// <param name="colorName">The name of the color</param>
        /// <returns>The corresponding ConsoleColor</returns>
        private static ConsoleColor ParseColorName(string colorName)
        {
            // Convert color name to ConsoleColor enum
            return colorName.ToLower() switch
            {
                "black" => ConsoleColor.Black,
                "blue" => ConsoleColor.Blue,
                "cyan" => ConsoleColor.Cyan,
                "darkblue" => ConsoleColor.DarkBlue,
                "darkcyan" => ConsoleColor.DarkCyan,
                "darkgray" => ConsoleColor.DarkGray,
                "darkgreen" => ConsoleColor.DarkGreen,
                "darkmagenta" => ConsoleColor.DarkMagenta,
                "darkred" => ConsoleColor.DarkRed,
                "darkyellow" => ConsoleColor.DarkYellow,
                "gray" => ConsoleColor.Gray,
                "green" => ConsoleColor.Green,
                "magenta" => ConsoleColor.Magenta,
                "red" => ConsoleColor.Red,
                "white" => ConsoleColor.White,
                "yellow" => ConsoleColor.Yellow,
                "orange" => ConsoleColor.Red, 
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Gets a string with color formatting for a token.
        /// </summary>
        /// <param name="token">The token to format</param>
        /// <returns>A string with color tags</returns>
        public static string GetTokenColor(Token token)
        {
            if (token == null)
                return string.Empty;

            // Escape any special characters in the token content

            if(ParserGlobal.IsTranslate)
            {
                if (token is KeywordToken)
                {
                    token.Content = KeywordToken.GetTranslatedKeyword(token.Content);
                }
            }

            // Use the token type to determine the color
            return token switch
            {
                KeywordToken _ => $"[color=blue]{token.Content}[/color]",
                IdentifierToken _ => $"[color=cyan]{token.Content}[/color]",
                NumberLiteralToken _ => $"[color=green]{token.Content}[/color]",
                FloatLiteralToken _ => $"[color=green]{token.Content}[/color]",
                StringToken _ => $"[color=orange]{token.Content}[/color]",
                LibraryToken _ => $"[color=orange]{token.Content}[/color]",
                OpenBraceToken _ => $"[color=magenta]{token.Content}[/color]",
                CloseBraceToken _ => $"[color=magenta]{token.Content}[/color]",
                ReturnToken _ => $"[color=magenta]{token.Content}[/color]",
                _ => $"[color=gray]{token.Content}[/color]"
            };
        }

        /// <summary>
        /// Escapes special characters in the content that could interfere with color tags
        /// </summary>
        /// <param name="content">The content to escape</param>
        /// <returns>Escaped content</returns>
        private static string EscapeContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Replace [ and ] with safe representations to avoid conflicts with color tags
            return content
                .Replace("[", "&#91;")
                .Replace("]", "&#93;");
        }

        /// <summary>
        /// Colorizes a list of tokens and concatenates them into a single string
        /// </summary>
        /// <param name="tokens">List of tokens to colorize</param>
        /// <returns>A string with colorized tokens</returns>
        public static string ColorizeTokens(IEnumerable<Token> tokens)
        {
            if (tokens == null)
                return string.Empty;

            var result = new StringBuilder();
            foreach (var token in tokens)
            {
                result.Append(GetTokenColor(token));
            }
            return result.ToString();
        }
    }
}