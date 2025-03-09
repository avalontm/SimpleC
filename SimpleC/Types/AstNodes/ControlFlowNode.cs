using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleC.Types.AstNodes
{
    public class ControlFlowNode : StatementSequenceNode
    {
        public string Type { get; private set; }
        public List<Token> Condition { get; private set; }

        public ControlFlowNode(string type) : base()
        {
            Type = type;
        }

        public void SetCondition(List<Token> condition)
        {
            Condition = condition;
        }
    }
}
