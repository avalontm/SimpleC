namespace SimpleC.Types.Tokens
{
    class LibraryToken : Token
    {
        public string Name { get; }

        public LibraryToken(string name) : base(name)
        {
            Name = name;
        }
    }
}
