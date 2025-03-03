namespace SimpleC.Types.Tokens
{
    class CharPointerToken : Token
    {
        public string PointerValue { get; }

        public CharPointerToken(string value) : base(value)
        {
            PointerValue = value;
        }
    }
}
