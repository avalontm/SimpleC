using SimpleC.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleC
{
    public static class VMEnumExtensions
    {
        public static bool IsValidOpCode(this byte value)
        {
            return Enum.IsDefined(typeof(OpCode), (int)value);
        }
    }
}
