using System;
using System.Text;

namespace SimpleC.VM.Instructions
{
    public static class StoreVariable
    {
        /// <summary>
        /// Ejecuta la instrucción Store Variable
        /// </summary>
        // Asegúrate que StoreVariable preserve el valor de string correctamente
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null)
            {
                throw new InvalidOperationException("VM instance not available");
            }

            // Determinar si estamos en un store global o local
            bool isGlobal = vm.CurrentOpcode == (byte)OpCode.StoreGlobal;

            // Avanzar al siguiente byte después del opcode
            vm.Ip++;

            // Leer el tipo de constante
            byte constantType = vm.Bytecode[vm.Ip++];

            // Leer el nombre de la variable con formato longitud+nombre
            string varName = ReadVarName(vm);

            // Verificar que haya algún valor en la pila
            if (vm.Stack.Count == 0)
            {
                vm.ReportError($"Stack is empty. Cannot store value in variable '{varName}'");
                return;
            }

            // Obtener el valor de la pila
            object value = vm.Stack.Pop();

            // Almacenar el valor en el contexto apropiado
            if (isGlobal)
            {
                vm.GlobalContext.Variables[varName] = value;
                vm.OnVariableChanged($"Global variable '{varName}' changed to:", value);
                // Añadir un mensaje detallado de depuración
                vm.OnDebugMessage($"StoreGlobal: '{varName}' = '{value}' (type: {value?.GetType().Name ?? "null"})");
            }
            else
            {
                vm.CurrentContext.Variables[varName] = value;
                vm.OnVariableChanged($"Local variable '{varName}' changed to:", value);
                // Añadir un mensaje detallado de depuración
                vm.OnDebugMessage($"Store: '{varName}' = '{value}' (type: {value?.GetType().Name ?? "null"})");
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