using System;

namespace SimpleC.Types.Tokens
{
    enum PreprocessorType
    {
        Include,
        Define,
        Ifdef,
        Ifndef,
        Else,
        Endif,
        Unknown
    }

    class PreprocessorToken : Token
    {
        public PreprocessorType Type { get; private set; }

        public PreprocessorToken(string content) : base(content)
        {
            if (!content.StartsWith("#"))
                throw new ArgumentException("Un PreprocessorToken debe comenzar con '#'.");

            Type = ParsePreprocessorType(content);
        }

        private PreprocessorType ParsePreprocessorType(string content)
        {
            if (content.StartsWith("#include")) return PreprocessorType.Include;
            if (content.StartsWith("#define")) return PreprocessorType.Define;
            if (content.StartsWith("#ifdef")) return PreprocessorType.Ifdef;
            if (content.StartsWith("#ifndef")) return PreprocessorType.Ifndef;
            if (content.StartsWith("#else")) return PreprocessorType.Else;
            if (content.StartsWith("#endif")) return PreprocessorType.Endif;
            return PreprocessorType.Unknown;
        }
    }
}
