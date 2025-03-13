using System.Diagnostics;
using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para almacenar un valor en una variable
    /// </summary>
    public static class StoreVariable
    {
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;
            var bytecode = vm.Bytecode;
            vm.Ip++; // Avanzar más allá del opcode Global

            // Leer tipo de variable
            if (vm.Ip >= bytecode.Count)
                vm.ReportError("Unexpected end of bytecode while reading global variable type", false);

            // Leer el tipo de constante
            byte typeCode = bytecode[vm.Ip++];
            ConstantType varType = (ConstantType)typeCode;

            // Validar que hay suficientes bytes para el tipo de dato
            int requiredBytes = varType switch
            {
                ConstantType.Integer => 4,
                ConstantType.Float => 4,
                ConstantType.String => 1, // Al menos el byte de longitud
                ConstantType.Bool => 1,
                ConstantType.Char => 1,
                _ => throw new Exception($"Unsupported constant type: {varType}")
            };

            if (vm.Ip + requiredBytes > bytecode.Count)
                vm.ReportError($"Unexpected end of bytecode while reading {varType} value", false);

            // Extraer el valor según el tipo
            object value = varType switch
            {
                ConstantType.Integer => BitConverter.ToInt32(bytecode.ToArray(), vm.Ip),
                ConstantType.Float => BitConverter.ToSingle(bytecode.ToArray(), vm.Ip),
                ConstantType.String => ReadString(vm),
                ConstantType.Bool => bytecode[vm.Ip] == 1,
                ConstantType.Char => (char)bytecode[vm.Ip],
                _ => throw new Exception($"Unsupported constant type: {varType}")
            };


            // Almacenar en la última variable registrada
            if (vm.CurrentContext.LastVariable == null)
                vm.ReportError("No variable defined to store value", false);

            vm.CurrentContext.Variables[vm.CurrentContext.LastVariable] = value;
            vm.OnDebugMessage($"Stored variable: {vm.CurrentContext.LastVariable} = {value}");
        }

        private static string ReadString(VirtualMachine vm)
        {
            byte length = vm.Bytecode[vm.Ip];
            return Encoding.UTF8.GetString(
                vm.Bytecode.ToArray(),
                vm.Ip + 1,
                length
            );
        }
    }
}