using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using System.Diagnostics;

namespace SimpleC.Types.AstNodes
{
    public class ControlFlowNode : StatementSequenceNode
    {
        public string Type { get; private set; }
        public List<Token> Condition { get; private set; }
        public bool IsSwitchCase { get; private set; } = false;
        public bool IsSwitchBlock { get; private set; } = false;
        public Token ColonToken { get; set; } = null; // Para guardar el token ":" en case y default

        public ControlFlowNode(string type)
        {
            Condition = new List<Token>();
            Type = type;
            NameAst = type;

            // Determinar automáticamente si es un case o default
            if (type.ToLowerInvariant() == "case" || type.ToLowerInvariant() == "default")
            {
                IsSwitchCase = true;
            }

            // Determinar si es un bloque switch
            if (type.ToLowerInvariant() == "switch")
            {
                IsSwitchBlock = true;
            }
        }

        public void SetCondition(List<Token> condition)
        {
            Condition = condition;

            // Para case/default, guardar el token de ":" si está presente
            if (IsSwitchCase && condition.Count > 0 && condition.Last().Content == ":")
            {
                ColonToken = condition.Last();
            }

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
                        throw new Exception($"Error en {KeywordToken.GetTranslatedKeyword(Type)}: Variable '{identifierToken.Content}' no encontrada: " +
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
                if (this.SubNodes.Count() > 0 && this.SubNodes.First() is ControlFlowNode flowNode)
                {
                    if (flowNode.Type == "if")
                    {
                        ColorParser.WriteLine($"{Indentation}[color=magenta]{KeywordToken.GetTranslatedKeyword(Type)}[/color]", false);
                    }
                    else
                    {
                        ColorParser.WriteLine($"{Indentation}[color=magenta]{KeywordToken.GetTranslatedKeyword(Type)}[/color]", true);
                    }
                }
                else
                {
                    ColorParser.WriteLine($"{Indentation}[color=magenta]{KeywordToken.GetTranslatedKeyword(Type)}[/color]");
                }
            }
            else if (Type == "case" || Type == "default")
            {
                if (conditions.Count > 0)
                {
                    conditions.RemoveAt(conditions.Count - 1);  // Elimina el último elemento de la lista (el ":")
                }
                string result = string.Join(" ", conditions);  // Une los elementos restantes con un espacio

                ColorParser.WriteLine($"{Indentation}[color=magenta]{KeywordToken.GetTranslatedKeyword(Type)}[/color] {result}");
            }
            else
            {
                ColorParser.WriteLine($"{Indentation}[color=magenta]{KeywordToken.GetTranslatedKeyword(Type)}[/color] {string.Join(" ", conditions)}");
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
            return $"{KeywordToken.GetTranslatedKeyword(Type)}";
        }
    }
}