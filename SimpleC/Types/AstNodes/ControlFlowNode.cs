using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using SimpleC.Utils;
using SimpleC.VM;
using System.Diagnostics;
using System.Text;

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

        public override List<byte> ByteCode()
        {
            List<byte> opCodes = new List<byte>();

            Debug.WriteLine($"Generating bytecode for {Type} statement");

            switch (Type.ToLowerInvariant())
            {
                case "if":
                    opCodes.AddRange(GenerateIfBytecode());
                    break;
                case "else":
                    opCodes.AddRange(GenerateElseBytecode());
                    break;
            }

            return opCodes;
        }

        private List<byte> GenerateIfBytecode()
        {
            List<byte> opCodes = new List<byte>();

            // Generar bytecode para la condición usando Condition
            if (Condition.Count > 0)
            {
                // Convertir los tokens de Condition a RPN (Notación Polaca Inversa)
                List<Token> rpnCondition = ConvertToRPN(Condition);
                GenerateRPNBytecode(rpnCondition, opCodes);
            }

            // Añadir opcode de salto condicional
            opCodes.Add((byte)OpCode.JumpIfFalse);

            // Generar bytecode para el cuerpo del if
            List<byte> bodyBytecode = GenerateBodyBytecode();

            // Añadir longitud del cuerpo
            opCodes.AddRange(BitConverter.GetBytes(bodyBytecode.Count));

            // Añadir bytecode del cuerpo del if
            opCodes.AddRange(bodyBytecode);

            return opCodes;
        }

        private List<byte> GenerateElseBytecode()
        {
            List<byte> opCodes = new List<byte>();

            // Depuración para rastrear el proceso de generación de bytecode del else
            Debug.WriteLine($"Generating bytecode for else block with {SubNodes.Count()} subnodes");

            // Generar bytecode para el cuerpo del else
            List<byte> bodyBytecode = GenerateBodyBytecode();

            // Si es un else seguido de un if (else if)
            if (SubNodes.FirstOrDefault() is ControlFlowNode elseIfNode &&
                elseIfNode.Type.ToLowerInvariant() == "if")
            {
                // Añadir salto incondicional antes del else if
                opCodes.Add((byte)OpCode.Jump);
                opCodes.AddRange(BitConverter.GetBytes(bodyBytecode.Count));
            }

            // Añadir bytecode del cuerpo del else
            opCodes.AddRange(bodyBytecode);

            return opCodes;
        }
        private List<byte> GenerateBodyBytecode()
        {
            List<byte> opCodes = new List<byte>();

            // Depuración para rastrear los nodos internos
            Debug.WriteLine($"Generating body bytecode for {Type} with {SubNodes.Count()} subnodes");

            // Generar bytecode para cada nodo hijo
            foreach (var node in SubNodes)
            {
                Debug.WriteLine($"Processing node type: {node.GetType().Name}");

                // Manejar diferentes tipos de nodos
                if (node is StatementSequenceNode statementNode)
                {
                    // Generar bytecode para el nodo de declaración
                    List<byte> nodeBytecode = GenerateStatementBytecode(statementNode);
                    opCodes.AddRange(nodeBytecode);
                }
            }

            return opCodes;
        }

        private List<byte> GenerateStatementBytecode(StatementSequenceNode node)
        {
            // Método para generar bytecode dependiendo del tipo de nodo
            Debug.WriteLine($"Generating statement bytecode for {node.GetType().Name}");

            if (node is ReturnNode returnNode)
            {
                return returnNode.ByteCode();
            }
            else if (node is MethodCallNode callNode)
            {
                // Si es una llamada a función (como printf)
                return callNode.ByteCode();
            }
            // Añadir más tipos de nodos según sea necesario

            // Si no se reconoce el tipo, devolver una lista vacía
            Debug.WriteLine($"Unhandled node type: {node.GetType().Name}");
            return new List<byte>();
        }

        private List<Token> ConvertToRPN(List<Token> infixTokens)
        {
            List<Token> output = new List<Token>();
            Stack<Token> operatorStack = new Stack<Token>();

            foreach (Token token in infixTokens)
            {
                if (token is IdentifierToken || token is NumberLiteralToken ||
                    token is FloatLiteralToken || token is StringToken ||
                    token is CharLiteralToken || token is BoolToken)
                {
                    // Valores directos a la salida
                    output.Add(token);
                }
                else if (token.Content == "(")
                {
                    // Paréntesis de apertura a la pila
                    operatorStack.Push(token);
                }
                else if (token.Content == ")")
                {
                    // Procesar hasta encontrar el paréntesis de apertura correspondiente
                    while (operatorStack.Count > 0 && operatorStack.Peek().Content != "(")
                    {
                        output.Add(operatorStack.Pop());
                    }

                    // Descartar el paréntesis de apertura
                    if (operatorStack.Count > 0 && operatorStack.Peek().Content == "(")
                    {
                        operatorStack.Pop();
                    }
                }
                else if (token is OperatorToken)
                {
                    // Para operadores, aplicar reglas de precedencia
                    while (operatorStack.Count > 0 &&
                           operatorStack.Peek() is OperatorToken stackOp &&
                           operatorStack.Peek().Content != "(" &&
                           GetPrecedence(stackOp) >= GetPrecedence((OperatorToken)token))
                    {
                        output.Add(operatorStack.Pop());
                    }

                    operatorStack.Push(token);
                }
            }

            // Mover los operadores restantes a la salida
            while (operatorStack.Count > 0)
            {
                output.Add(operatorStack.Pop());
            }

            return output;
        }

        private int GetPrecedence(OperatorToken op)
        {
            switch (op.Content)
            {
                case "==":
                case "!=":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    return 3;
                case "+":
                case "-":
                    return 2;
                case "*":
                case "/":
                    return 1;
                default:
                    return 0;
            }
        }

        private void GenerateRPNBytecode(List<Token> rpnTokens, List<byte> opCodes)
        {
            foreach (Token token in rpnTokens)
            {
                if (token is IdentifierToken || token is NumberLiteralToken ||
                    token is FloatLiteralToken || token is StringToken ||
                    token is CharLiteralToken || token is BoolToken)
                {
                    // Generar bytecode para operandos
                    GenerateTokenBytecode(token, opCodes);
                }
                else if (token is OperatorToken opToken)
                {
                    // Generar bytecode para operadores
                    switch (opToken.Content)
                    {
                        case "+":
                            opCodes.Add((byte)OpCode.Add);
                            break;
                        case "-":
                            opCodes.Add((byte)OpCode.Sub);
                            break;
                        case "*":
                            opCodes.Add((byte)OpCode.Mul);
                            break;
                        case "/":
                            opCodes.Add((byte)OpCode.Div);
                            break;
                        case "==":
                            opCodes.Add((byte)OpCode.Equal);
                            break;
                        case "!=":
                            opCodes.Add((byte)OpCode.NotEqual);
                            break;
                        case "<":
                            opCodes.Add((byte)OpCode.Less);
                            break;
                        case "<=":
                            opCodes.Add((byte)OpCode.LessEqual);
                            break;
                        case ">":
                            opCodes.Add((byte)OpCode.Greater);
                            break;
                        case ">=":
                            opCodes.Add((byte)OpCode.GreaterEqual);
                            break;
                        default:
                            Debug.WriteLine($"Unsupported operator: {opToken.Content}");
                            break;
                    }
                }
            }
        }

        private void GenerateTokenBytecode(Token token, List<byte> opCodes)
        {
            if (token is IdentifierToken identToken)
            {
                // Verificar si es una variable local o global
                string varName = identToken.Content;
                bool isLocal = this.Verify(varName);
                bool isGlobal = !isLocal && ParserGlobal.Verify(varName);

                if (isLocal)
                {
                    // Cargar variable local
                    opCodes.Add((byte)OpCode.Load);
                    byte[] varNameBytes = Encoding.UTF8.GetBytes(varName);
                    opCodes.Add((byte)varNameBytes.Length);
                    opCodes.AddRange(varNameBytes);
                }
                else if (isGlobal)
                {
                    // Cargar variable global
                    opCodes.Add((byte)OpCode.LoadGlobal);
                    byte[] varNameBytes = Encoding.UTF8.GetBytes(varName);
                    opCodes.Add((byte)varNameBytes.Length);
                    opCodes.AddRange(varNameBytes);
                }
                else
                {
                    // Variable no encontrada
                    Debug.WriteLine($"WARNING: Variable '{varName}' not found");
                    opCodes.Add((byte)OpCode.LoadC);
                    opCodes.Add((byte)ConstantType.Integer);
                    opCodes.AddRange(BitConverter.GetBytes(0));
                }
            }
            else if (token is NumberLiteralToken numToken)
            {
                // Cargar constante entera
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Integer);
                opCodes.AddRange(BitConverter.GetBytes((int)numToken.Numero));
            }
            else if (token is FloatLiteralToken floatToken)
            {
                // Cargar constante flotante
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Float);
                opCodes.AddRange(BitConverter.GetBytes(floatToken.Numero));
            }
            else if (token is StringToken strToken)
            {
                // Cargar constante de cadena
                string strValue = strToken.Content;
                // Eliminar comillas si las tiene
                if (strValue.StartsWith("\"") && strValue.EndsWith("\""))
                    strValue = strValue.Substring(1, strValue.Length - 2);

                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.String);
                byte[] strBytes = Encoding.UTF8.GetBytes(strValue);
                opCodes.Add((byte)strBytes.Length);
                opCodes.AddRange(strBytes);
            }
            else if (token is BoolToken boolToken)
            {
                // Cargar constante booleana
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Bool);
                opCodes.Add((byte)(boolToken.Value ? 1 : 0));
            }
            else if (token is CharLiteralToken charToken)
            {
                // Cargar constante de carácter
                char charValue = charToken.Content[1]; // Extraer el char de las comillas
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Char);
                opCodes.Add((byte)charValue);
            }
            else
            {
                Debug.WriteLine($"Unsupported token type: {token.GetType().Name}");
                // Valor por defecto
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Integer);
                opCodes.AddRange(BitConverter.GetBytes(0));
            }
        }


    }
}