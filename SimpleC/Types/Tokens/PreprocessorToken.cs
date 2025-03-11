using System;

namespace SimpleC.Types.Tokens
{
    class PreprocessorToken : Token
    {
        public PreprocessorToken(string content, int line, int column) : base(content, line, column)
        {
            if (!content.StartsWith("#"))
                throw new ArgumentException("Un PreprocessorToken debe comenzar con '#'.");
        }

    }
}
