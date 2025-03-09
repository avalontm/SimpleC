namespace SimpleC.Types.Tokens
{
    public class NumberLiteralToken : Token
    {
        public int Numero { get; } // Propiedad de solo lectura

        public NumberLiteralToken(string contenido, int linea, int columna) : base(contenido, linea, columna)
        {
            if (!int.TryParse(contenido, out int numero))
                throw new ArgumentException("El contenido no es un número entero válido.", nameof(contenido));

            Numero = numero;
        }
    }
}
