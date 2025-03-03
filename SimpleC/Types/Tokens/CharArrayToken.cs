using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleC.Types.Tokens
{
    class CharArrayToken : Token
    {
        public char Value { get; }
        public int? Size { get; }
        public CharArrayToken(char value, int? size ) : base(value.ToString())
        {
            Value = value;
            Size = size;
        }
    }
}
