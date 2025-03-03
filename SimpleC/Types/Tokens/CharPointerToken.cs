namespace SimpleC.Types.Tokens
{
    class CharPointerToken : Token
    {
        public char Value { get; }

        public CharPointerToken(char value) : base(value.ToString())
        {
            Value = value;
        }
    }
}
