using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para cargar una variable en la pila
    /// </summary>
    public static class LoadVariable
    {
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;
            vm.Ip++; // Avanzar más allá del opcode

            // Leer tipo de variable
            if (vm.Ip >= vm.Bytecode.Count)
                vm.ReportError("Unexpected end of bytecode while reading variable type", false);

            byte typeCode = vm.Bytecode[vm.Ip++];
            ConstantType varType = (ConstantType)typeCode;

            // Leer nombre de variable
            if (vm.Ip >= vm.Bytecode.Count)
                vm.ReportError("Unexpected end of bytecode while reading variable name length", false);

            byte nameLength = vm.Bytecode[vm.Ip++];

            if (vm.Ip + nameLength > vm.Bytecode.Count)
                vm.ReportError("Unexpected end of bytecode while reading variable name", false);

            string varName = Encoding.UTF8.GetString(vm.Bytecode.ToArray(), vm.Ip, nameLength);

            // Intentar obtener valor de contexto actual
            object value = null;
            bool found = vm.CurrentContext.Variables.TryGetValue(varName, out value);

            // Si no se encuentra en el contexto actual, buscar en contexto global
            if (!found)
            {
                found = vm.GlobalContext.Variables.TryGetValue(varName, out value);
            }

            // Si no se encuentra en ningún contexto, crear en contexto local
            if (!found)
            {
                vm.CurrentContext.SetVariable(varName);
           
                // Registrar la variable local con un valor por defecto
                object defaultValue = vm.GetDefaultValueForType(varType);
                vm.CurrentContext.Variables[varName] = defaultValue;
            }


            // Avanzar más allá del nombre de variable
            vm.Ip += nameLength;

            vm.OnDebugMessage($"variable registered: {varName} of type {varType}");
        }
    }
}