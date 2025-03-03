using System;

namespace SimpleC.Types.Tokens
{
    class StatementSperatorToken : Token
    {
        public StatementSperatorToken(string content) : base(content)
        {
            if (content != ";")
                throw new ArgumentException("The content is no statement seperator.", "content");
        }
    }
}
