using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleC.Types.Tokens
{
    internal class NewLineToken : Token
    {
        public NewLineToken(string content, int line, int column) : base(content, line, column)
        {
           Debug.WriteLine("NewLine");
        }
    }
}
