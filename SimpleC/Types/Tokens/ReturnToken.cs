using System;
using System.Diagnostics;

namespace SimpleC.Types.Tokens
{
    class ReturnToken : Token
    {
        public ReturnToken(string content, int line, int column) : base(content, line, column)
        {
            Debug.WriteLine(content);

            // Verificar si el contenido no es "return"
            if (content != "return")
            {
                // Lanzar excepción con mensaje detallado incluyendo línea y columna
                throw new ArgumentException(
                    $"El contenido '{content}' no es un separador de declaración válido. Se esperaba 'return'. " +
                    $"Línea: {line}, Columna: {column}", "content");
            }
        }
    }
}
