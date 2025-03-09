namespace SimpleC.Types.Tokens
{
    public class FloatLiteralToken : Token
    {
        public float Numero { get; }

        public FloatLiteralToken(string contenido, int linea, int columna) : base(contenido, linea, columna)
        {
            if (!float.TryParse(contenido, out float numero))
                throw new ArgumentException("El contenido no es un número flotante válido.", nameof(contenido));

            Numero = numero;
        }
    }
}
