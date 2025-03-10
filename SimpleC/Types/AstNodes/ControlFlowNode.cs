using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    public class ControlFlowNode : StatementSequenceNode
    {
        public string Type { get; private set; }
        public List<Token> Condition { get; private set; }

        public ControlFlowNode(string type) 
        {
            Condition = new List<Token>();
            Type = type;
        }

        public void SetCondition(List<Token> condition)
        {
            Condition = condition;

            Debug.Write($"{Type.ToLowerInvariant()}");
            foreach (var _condition in Condition)
            {
                Debug.Write($" {_condition.Content}");
            }
            Debug.WriteLine("");
        }

        public override void Generate()
        {
            base.Generate();

            List<string> conditions = new List<string>();

            foreach (var _condition in Condition)
            {
                conditions.Add(ColorParser.GetTokenColor(_condition));
            }

            if (Type == "else")
            {
                if(this.SubNodes.First() is ControlFlowNode flowNode)
                {
                    if (flowNode.Type == "if")
                    {
                        Debug.WriteLine(flowNode.Type); 
                        ColorParser.WriteLine($"{Indentation}[color=magenta]{Type.ToLowerInvariant()}[/color]", false);
                    }else
                    {
                        ColorParser.WriteLine($"{Indentation}[color=magenta]{Type.ToLowerInvariant()}[/color]", true);
                    }
                }else
                { 
                    ColorParser.WriteLine($"{Indentation}[color=magenta]{Type.ToLowerInvariant()}[/color]");
                }
            }
            else
            {
                ColorParser.WriteLine($"{Indentation}[color=magenta]{Type.ToLowerInvariant()}[/color] {string.Join(" ", conditions)}");
            }
            //Llamamos el BlockNode
            foreach (var node in this.SubNodes)
            {
                if(node is ControlFlowNode flowNode)
                {
                    if (Type == "else" && flowNode.Type == "if")
                    {
                        flowNode.SetIndent(" ");
                        flowNode.Indent = Indent;
                        flowNode.Generate();
                    }else
                    {
                        node.Indent = Indent;
                        node.Generate();
                    }
                }
                else
                {
                    node.Indent = Indent;
                    node.Generate();
                }
            }
        }

        public override string ToString()
        {
            return $"{Type.ToLowerInvariant()}";
        }
    }
}
