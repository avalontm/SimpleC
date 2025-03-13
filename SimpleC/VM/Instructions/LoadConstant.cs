using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para cargar una constante en la pila
    /// </summary>
    public static class LoadConstant
    {
        /// <summary>
        /// Ejecuta la instrucción LoadC que carga una constante en la pila
        /// </summary>
        public static void Execute()
        {
            VirtualMachine.Instance.Ip++; // Avanzar al byte de tipo de constante
            if (VirtualMachine.Instance.Ip >= VirtualMachine.Instance.Bytecode.Count)
                VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading constant type");

            ConstantType constantType = (ConstantType)VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip];
            VirtualMachine.Instance.OnDebugMessage($"LoadConstant: {constantType}");

            switch (constantType)
            {
                case ConstantType.Integer:
                    VirtualMachine.Instance.Ip++; // Avanzar al inicio del valor entero
                    if (VirtualMachine.Instance.Ip + 3 >= VirtualMachine.Instance.Bytecode.Count)
                        VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading integer value");

                    int intValue = BitConverter.ToInt32(VirtualMachine.Instance.Bytecode.ToArray(), VirtualMachine.Instance.Ip);
                    VirtualMachine.Instance.Stack.Push(intValue);
                    VirtualMachine.Instance.OnDebugMessage($"Pushed integer: {intValue}");
                    VirtualMachine.Instance.Ip += 3; // Avanzar a la siguiente instrucción
                    break;

                case ConstantType.String:
                    VirtualMachine.Instance.Ip++; // Avanzar al byte de longitud de string
                    if (VirtualMachine.Instance.Ip >= VirtualMachine.Instance.Bytecode.Count)
                        VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading string length");

                    ushort stringLength;
                    byte lengthByte = VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip];

                    if (lengthByte > 127) // Usando el bit alto como indicador para longitud de 2 bytes
                    {
                        if (VirtualMachine.Instance.Ip + 1 >= VirtualMachine.Instance.Bytecode.Count)
                            VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading string length (2 bytes)");

                        stringLength = BitConverter.ToUInt16(VirtualMachine.Instance.Bytecode.ToArray(), VirtualMachine.Instance.Ip);
                        VirtualMachine.Instance.Ip += 2; // Saltar 2 bytes de longitud
                    }
                    else
                    {
                        stringLength = lengthByte;
                        VirtualMachine.Instance.Ip++; // Saltar 1 byte de longitud
                    }

                    if (VirtualMachine.Instance.Ip + stringLength > VirtualMachine.Instance.Bytecode.Count)
                        VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading string data");

                    string strValue = Encoding.UTF8.GetString(VirtualMachine.Instance.Bytecode.ToArray(), VirtualMachine.Instance.Ip, stringLength);
                    VirtualMachine.Instance.Stack.Push(strValue);
                    VirtualMachine.Instance.OnDebugMessage($"Pushed string: \"{strValue}\"");
                    VirtualMachine.Instance.Ip += stringLength - 1; // Ajustar para el incremento del bucle principal
                    break;

                case ConstantType.Float:
                    VirtualMachine.Instance.Ip++; // Avanzar al inicio del valor float
                    if (VirtualMachine.Instance.Ip + 3 >= VirtualMachine.Instance.Bytecode.Count)
                        VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading float value");

                    float floatValue = BitConverter.ToSingle(VirtualMachine.Instance.Bytecode.ToArray(), VirtualMachine.Instance.Ip);
                    VirtualMachine.Instance.Stack.Push(floatValue);
                    VirtualMachine.Instance.OnDebugMessage($"Pushed float: {floatValue}");
                    VirtualMachine.Instance.Ip += 3; // Saltar bytes
                    break;

                case ConstantType.Char:
                    VirtualMachine.Instance.Ip++; // Avanzar al valor char
                    if (VirtualMachine.Instance.Ip >= VirtualMachine.Instance.Bytecode.Count)
                        VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading char value");

                    char charValue = (char)VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip];
                    VirtualMachine.Instance.Stack.Push(charValue);
                    VirtualMachine.Instance.OnDebugMessage($"Pushed char: '{charValue}'");
                    break;

                case ConstantType.Bool:
                    VirtualMachine.Instance.Ip++; // Avanzar al valor bool
                    if (VirtualMachine.Instance.Ip >= VirtualMachine.Instance.Bytecode.Count)
                        VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading bool value");

                    bool boolValue = VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip] != 0;
                    VirtualMachine.Instance.Stack.Push(boolValue);
                    VirtualMachine.Instance.OnDebugMessage($"Pushed boolean: {boolValue}");
                    break;

                case ConstantType.Void:
                    VirtualMachine.Instance.OnDebugMessage("Void type detected, nothing pushed to stack");
                    break;

                default:
                    VirtualMachine.Instance.ReportError($"Unknown constant type: {constantType}");
                    break;
            }

            VirtualMachine.Instance.Ip++; // Avanzar a la siguiente instrucción
        }
    }
}