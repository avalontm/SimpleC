using SimpleC.Parsing;
using SimpleC.Types.Tokens;

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
            NameAst = type;
        }

        public void SetCondition(List<Token> condition)
        {
            Condition = condition;
            CheckConditionInGlobals(); // Verifica las variables al establecer la condición
        }

        private void CheckConditionInGlobals()
        {
            foreach (var token in Condition)
            {
                if (token is IdentifierToken identifierToken)
                {
                    if (!this.Verify(identifierToken.Content) && !ParserGlobal.Verify(identifierToken.Content))
                    {
                        throw new Exception($"Error en {Type.ToLowerInvariant()}: Variable '{identifierToken.Content}' no encontrada: " +
                                            $"(Línea: {identifierToken.Line}, Columna: {identifierToken.Column})");
                    }
                }
            }
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
                if (this.SubNodes.First() is ControlFlowNode flowNode)
                {
                    if (flowNode.Type == "if")
                    {
                        ColorParser.WriteLine($"{Indentation}[color=magenta]{Type.ToLowerInvariant()}[/color]", false);
                    }
                    else
                    {
                        ColorParser.WriteLine($"{Indentation}[color=magenta]{Type.ToLowerInvariant()}[/color]", true);
                    }
                }
                else
                {
                    ColorParser.WriteLine($"{Indentation}[color=magenta]{Type.ToLowerInvariant()}[/color]");
                }
            }
            else if (Type == "case" || Type == "default")
            {
                if (conditions.Count > 0)
                {
                    conditions.RemoveAt(conditions.Count - 1);  // Elimina el último elemento de la lista
                }
                string result = string.Join(" ", conditions);  // Une los elementos restantes con un espacio

                ColorParser.WriteLine($"{Indentation}[color=magenta]{Type.ToLowerInvariant()}[/color] {result}");
            }
            else
            {
                ColorParser.WriteLine($"{Indentation}[color=magenta]{Type.ToLowerInvariant()}[/color] {string.Join(" ", conditions)}");
            }

            // Llamamos al BlockNode
            foreach (var node in this.SubNodes)
            {
                if (node is ControlFlowNode flowNode)
                {
                    if (Type == "else" && flowNode.Type == "if")
                    {
                        flowNode.SetIndent(" ");
                        flowNode.Indent = Indent;
                        flowNode.Generate();
                    }
                    else
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
