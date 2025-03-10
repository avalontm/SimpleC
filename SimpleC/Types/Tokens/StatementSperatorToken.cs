using System;

namespace SimpleC.Types.Tokens
{
    class StatementSperatorToken : Token
    {
        public StatementSperatorToken(string content, int line, int column) : base(content, line, column)
        {
            if (content != ";" && content != ":")
                throw new ArgumentException("The content is no statement seperator.", "content");
        }
    }
}
