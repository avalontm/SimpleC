using SimpleC.Parsing;
using SimpleC.Types.Tokens;
using SimpleC.Utils;
using SimpleC.VM;
using System.Diagnostics;
using System.Text;

namespace SimpleC.Types.AstNodes
{
    internal class ReturnNode : StatementSequenceNode
    {
        public List<Token> Values { get; }
        public ReturnNode(List<Token> tokens)
        {
            NameAst = "Regresar";
            Values = new List<Token>();
            bool hasSemicolon = false;
            bool hasOtherValues = false;
            foreach (var token in tokens)
            {
                if (token is not KeywordToken keywordToken)
                {
                    // Verificar si es un punto y coma (token de cierre)
                    if (token.Content == ";")
                    {
                        hasSemicolon = true;
                    }
                    else
                    {
                        hasOtherValues = true;
                    }
                    Values.Add(token);
                }
            }
            // Verificar que tenga el token de cierre (;)
            if (!hasSemicolon)
            {
                var lastToken = tokens.LastOrDefault();
                int line = lastToken?.Line ?? 0;
                int column = lastToken?.Column ?? 0;
                throw new Exception($"Error: Falta el punto y coma (;) al final del retorno: Linea {line}, Columna {column}.");
            }
            // Verificar que haya al menos un valor además del punto y coma
            if (!hasOtherValues)
            {
                // Obtener la línea y columna del token 'return'
                var returnToken = tokens.FirstOrDefault(t => t is KeywordToken);
                int line = returnToken?.Line ?? 0;
                int column = returnToken?.Column ?? 0;
                throw new Exception($"Error: Declaración de retorno sin valor: Linea {line}, Columna {column}. Se necesita retornar al menos un valor.");
            }
   
        }

        public void SetOwner(StatementSequenceNode node)
        {
            Owner = (node as BlockNode).Owner;
            this.SetParameters(Owner, node.GetParameters());
            foreach (var token in Values)
            {
                if (token is IdentifierToken identifierToken)
                {
                    if (!node.Verify(identifierToken.Content))
                    {
                        throw new Exception($"La variable `{token.Content}` no se encontro: Linea {token.Line}, Columna {token.Column}.");
                    }
                }
            }
        }

        public override void Generate()
        {
            base.Generate();
            List<string> values = new List<string>();
            foreach (var value in Values)
            {
                if (value is not KeywordToken keywordToken)
                {
                    values.Add(ColorParser.GetTokenColor(value));
                }
            }
            ColorParser.WriteLine($"{Indentation}[color=magenta]{KeywordToken.GetTranslatedKeyword("return")}[/color] {string.Join(" ", values)}");
        }

        public override List<byte> ByteCode()
        {
            List<byte> opCodes = new List<byte>();

            Debug.WriteLine($"Generating bytecode for return statement");

            // Filtrar los tokens que son parte de la expresión (eliminar el punto y coma final)
            var expressionTokens = Values.Where(t => t.Content != ";").ToList();

            if (expressionTokens.Count > 0)
            {
                if (expressionTokens.Count == 1)
                {
                    // Caso simple: un solo token (variable o literal)
                    GenerateTokenBytecode(expressionTokens[0], opCodes);
                }
                else
                {
                    // Expresión compleja - usar el algoritmo Shunting Yard para convertir a RPN
                    List<Token> rpnTokens = ConvertToRPN(expressionTokens);
                    GenerateRPNBytecode(rpnTokens, opCodes);
                }
            }
            else
            {
                // Return sin expresión, cargar 0 por defecto
                Debug.WriteLine("Return without expression, loading default value 0");
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Integer);
                opCodes.AddRange(BitConverter.GetBytes(0));
            }

            // Añadir el opcode Return
            Debug.WriteLine("Adding Return opcode");
            opCodes.Add((byte)OpCode.Return);

            return opCodes;
        }

        // Genera bytecode para un solo token
        private void GenerateTokenBytecode(Token token, List<byte> opCodes)
        {
            if (token is IdentifierToken identToken)
            {
                // Verificar si el identificador es una variable local o global
                string varName = identToken.Content;
                bool isLocal =  this.Verify(varName);
                bool isGlobal = !isLocal && ParserGlobal.Verify(varName);

                object value = this.Get(varName);
                Debug.WriteLine($"Return identifier: {varName}, IsLocal: {isLocal}, IsGlobal: {isGlobal}");

                if (isLocal)
                {
                    // Variable local
                    opCodes.Add((byte)OpCode.Load);
                    byte[] varNameBytes = Encoding.UTF8.GetBytes(varName);
                    opCodes.Add((byte)varNameBytes.Length);
                    opCodes.AddRange(varNameBytes);
                }
                else if (isGlobal)
                {
                    // Variable global
                    opCodes.Add((byte)OpCode.LoadGlobal);
                    byte[] varNameBytes = Encoding.UTF8.GetBytes(varName);
                    opCodes.Add((byte)varNameBytes.Length);
                    opCodes.AddRange(varNameBytes);
                }
                else
                {
                    // Variable no encontrada, esto no debería ocurrir si la verificación previa funciona
                    Debug.WriteLine($"WARNING: Variable '{varName}' not found in any context");
                    opCodes.Add((byte)OpCode.LoadC);
                    opCodes.Add((byte)ConstantType.Integer);
                    opCodes.AddRange(BitConverter.GetBytes(0));
                }
            }
            else if (token is NumberLiteralToken numToken)
            {
                // Return 123;
                Debug.WriteLine($"Return number: {numToken.Numero}");
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Integer);
                opCodes.AddRange(BitConverter.GetBytes((int)numToken.Numero));
            }
            else if (token is StringToken strToken)
            {
                // Return "string";
                string strValue = strToken.Content;
                // Eliminar comillas si las tiene
                if (strValue.StartsWith("\"") && strValue.EndsWith("\""))
                    strValue = strValue.Substring(1, strValue.Length - 2);

                Debug.WriteLine($"Return string: {strValue}");
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.String);
                byte[] strBytes = Encoding.UTF8.GetBytes(strValue);
                opCodes.Add((byte)strBytes.Length);
                opCodes.AddRange(strBytes);
            }
            else if (token is FloatLiteralToken floatToken)
            {
                // Return 3.14;
                Debug.WriteLine($"Return float: {floatToken.Numero}");
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Float);
                opCodes.AddRange(BitConverter.GetBytes(floatToken.Numero));
            }
            else if (token is BoolToken boolToken)
            {
                // Return true/false;
                Debug.WriteLine($"Return boolean: {boolToken.Value}");
                opCodes.Add((byte)OpCode.LoadC);
                opCodes.Add((byte)ConstantType.Bool);
                opCodes.Add((byte)(boolToken.Value ? 1 : 0));
            }
            else if (token is CharLiteralToken charToken)
            {
                // Return 'a';
                char charValue = charToken.Content[1]; // Extraer el char de las comillas
                Debug.WriteLine($"Return char: {charValue}");
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

        // Convierte la expresión infija a notación polaca inversa (RPN)
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

        // Obtener la precedencia de un operador
        private int GetPrecedence(OperatorToken op)
        {
            switch (op.Content)
            {
                case "+":
                case "-":
                    return 1;
                case "*":
                case "/":
                    return 2;
                default:
                    return 0;
            }
        }

        // Genera bytecode para una expresión RPN
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
                        default:
                            Debug.WriteLine($"Unsupported operator: {opToken.Content}");
                            break;
                    }
                }
            }
        }
    }
}