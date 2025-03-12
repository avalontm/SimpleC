using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SimpleC.Types.Tokens
{
    internal class BoolToken : Token
    {
        public bool Value { get; internal set; }

        public BoolToken(string content, int line, int column) : base(content, line, column)
        {
            if (!bool.TryParse(content, out bool value))
                throw new ArgumentException("El contenido no es un booleano válido.", nameof(content));

            Value = value;
        }
       
    }
}
