using System;
using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para cargar constantes en la pila
    /// </summary>
    public static class LoadConstant
    {
        /// <summary>
        /// Ejecuta la instrucción LoadC que carga una constante en la pila
        /// </summary>
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null)
            {
                throw new InvalidOperationException("VM instance not available");
            }

            // Avanzar al siguiente byte después del opcode
            vm.Ip++;

            // Leer el tipo de constante
            if (vm.Ip >= vm.Bytecode.Count)
            {
                vm.ReportError("Unexpected end of bytecode while reading constant type");
                return;
            }

            byte constantType = vm.Bytecode[vm.Ip++];

            // Cargar valor según el tipo
            object value = ReadConstantValue(vm, (ConstantType)constantType);

            // Poner el valor en la pila
            vm.Stack.Push(value);
            vm.OnDebugMessage($"Loaded constant value: {value} of type {(ConstantType)constantType}");
        }

        /// <summary>
        /// Lee el valor de una constante según su tipo
        /// </summary>
        private static object ReadConstantValue(VirtualMachine vm, ConstantType type)
        {
            switch (type)
            {
                case ConstantType.Integer:
                    // Leer entero (4 bytes)
                    if (vm.Ip + 4 > vm.Bytecode.Count)
                    {
                        vm.ReportError("Unexpected end of bytecode while reading integer value");
                        return 0;
                    }

                    byte[] intBytes = new byte[4];
                    for (int i = 0; i < 4; i++)
                    {
                        intBytes[i] = vm.Bytecode[vm.Ip++];
                    }

                    return BitConverter.ToInt32(intBytes, 0);

                case ConstantType.Float:
                    // Leer float (4 bytes)
                    if (vm.Ip + 4 > vm.Bytecode.Count)
                    {
                        vm.ReportError("Unexpected end of bytecode while reading float value");
                        return 0.0f;
                    }

                    byte[] floatBytes = new byte[4];
                    for (int i = 0; i < 4; i++)
                    {
                        floatBytes[i] = vm.Bytecode[vm.Ip++];
                    }

                    return BitConverter.ToSingle(floatBytes, 0);

                case ConstantType.String:
                    // Leer longitud del string
                    if (vm.Ip >= vm.Bytecode.Count)
                    {
                        vm.ReportError("Unexpected end of bytecode while reading string length");
                        return string.Empty;
                    }

                    byte strLength = vm.Bytecode[vm.Ip++];

                    // Leer bytes del string
                    if (vm.Ip + strLength > vm.Bytecode.Count)
                    {
                        vm.ReportError($"Unexpected end of bytecode while reading string (expected {strLength} bytes)");
                        return string.Empty;
                    }

                    byte[] strBytes = new byte[strLength];
                    for (int i = 0; i < strLength; i++)
                    {
                        strBytes[i] = vm.Bytecode[vm.Ip++];
                    }

                    return Encoding.UTF8.GetString(strBytes);

                case ConstantType.Bool:
                    // Leer bool (1 byte)
                    if (vm.Ip >= vm.Bytecode.Count)
                    {
                        vm.ReportError("Unexpected end of bytecode while reading boolean value");
                        return false;
                    }

                    byte boolByte = vm.Bytecode[vm.Ip++];
                    return boolByte != 0;

                case ConstantType.Char:
                    // Leer char (1 byte)
                    if (vm.Ip >= vm.Bytecode.Count)
                    {
                        vm.ReportError("Unexpected end of bytecode while reading char value");
                        return '\0';
                    }

                    byte charByte = vm.Bytecode[vm.Ip++];
                    return (char)charByte;

                default:
                    vm.ReportError($"Unknown constant type: {type}");
                    return null;
            }
        }
    }
}