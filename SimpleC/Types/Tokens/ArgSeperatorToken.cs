using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleC.Types.Tokens
{
    class ArgSeperatorToken : Token
    {
        public ArgSeperatorToken(string content, int line, int column) : base(content, line, column)
        {
            if (content != ",")
                throw new ArgumentException("The content is no argument seperator.", "content");
        }
    }
}
