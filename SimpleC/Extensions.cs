using SimpleC.Types.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleC
{
    public static class Extensions
    {
        public static string ToLowerString(this VariableType type)
        {
            return type.ToString().ToLower();
        }
    }
}
