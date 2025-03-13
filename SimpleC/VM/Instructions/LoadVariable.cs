using System;
using System.Text;

namespace SimpleC.VM.Instructions
{
    public static class LoadVariable
    {
        /// <summary>
        /// Ejecuta la instrucción Load Variable
        /// </summary>
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null)
            {
                throw new InvalidOperationException("VM instance not available");
            }

            // Determinar si estamos en un load global o local
            bool isGlobal = vm.CurrentOpcode == (byte)OpCode.LoadGlobal;

            // Avanzar al siguiente byte después del opcode
            vm.Ip++;

            // Leer el nombre de la variable con formato longitud+nombre
            string varName = ReadVarName(vm);

            // Check if it's a function
            if (vm.FunctionTable.ContainsKey(varName))
            {
                // Push function name to stack
                vm.Stack.Push(varName);
                vm.OnDebugMessage($"Loaded function '{varName}'");
                return;
            }

            // Buscar la variable en el contexto apropiado
            if (isGlobal)
            {
                // Buscar en contexto global
                if (vm.GlobalContext.Variables.TryGetValue(varName, out object value))
                {
                    // Poner el valor en la pila
                    vm.Stack.Push(value ?? vm.GetDefaultValueForType(ConstantType.Void));
                    vm.OnDebugMessage($"Loaded global variable '{varName}' with value: {value}");
                }
                else
                {
                    vm.ReportError($"Global variable '{varName}' not found");
                }
            }
            else
            {
                // Buscar en contexto local primero
                if (vm.CurrentContext.Variables.TryGetValue(varName, out object value))
                {
                    // Poner el valor en la pila
                    vm.Stack.Push(value ?? vm.GetDefaultValueForType(ConstantType.Void));
                    vm.OnDebugMessage($"Loaded local variable '{varName}' with value: {value}");
                }
                // Si no se encuentra en local, buscar en global como fallback
                else if (vm.GlobalContext.Variables.TryGetValue(varName, out object globalValue))
                {
                    // Poner el valor global en la pila
                    vm.Stack.Push(globalValue ?? vm.GetDefaultValueForType(ConstantType.Void));
                    vm.OnDebugMessage($"Loaded global variable '{varName}' with value: {globalValue} via local lookup");
                }
                else
                {
                    vm.ReportError($"Variable '{varName}' not found in any context");
                }
            }
        }

        /// <summary>
        /// Lee el nombre de la variable del bytecode según el formato generado
        /// </summary>
        private static string ReadVarName(VirtualMachine vm)
        {
            // Leer longitud del nombre (1 byte)
            if (vm.Ip >= vm.Bytecode.Count)
            {
                vm.ReportError("Unexpected end of bytecode while reading variable name length");
                return string.Empty;
            }

            byte nameLength = vm.Bytecode[vm.Ip++];

            // Verificar que hay suficientes bytes para el nombre
            if (vm.Ip + nameLength > vm.Bytecode.Count)
            {
                vm.ReportError($"Unexpected end of bytecode while reading variable name (expected {nameLength} bytes)");
                return string.Empty;
            }

            // Leer los bytes del nombre
            byte[] nameBytes = new byte[nameLength];
            for (int i = 0; i < nameLength; i++)
            {
                nameBytes[i] = vm.Bytecode[vm.Ip++];
            }

            return Encoding.UTF8.GetString(nameBytes);
        }
    }
}
