using System.Text;
using System.Diagnostics;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para registrar una función en la tabla de funciones
    /// </summary>
    public static class RegisterFunction
    {
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null)
                return;

            int funcStart = vm.Ip; // Guardar posición Mark
            vm.Ip++; // Avanzar después del Mark

            try
            {
                // Leer longitud del nombre de la función
                if (vm.Ip >= vm.Bytecode.Count)
                {
                    vm.ReportError("Unexpected end of bytecode while reading function name length", false);
                    return;
                }

                byte nameLength = vm.Bytecode[vm.Ip++];
                if (nameLength == 0 || nameLength > 50)
                {
                    vm.ReportError($"Invalid function name length: {nameLength}", false);
                    return;
                }

                // Leer nombre de la función
                if (vm.Ip + nameLength > vm.Bytecode.Count)
                {
                    vm.ReportError($"Not enough bytes for function name (need {nameLength} bytes)", false);
                    return;
                }

                byte[] nameBytes = new byte[nameLength];
                for (int i = 0; i < nameLength; i++)
                {
                    nameBytes[i] = vm.Bytecode[vm.Ip++];
                }

                // Verificar que el nombre sea ASCII válido
                bool isValidName = nameBytes.All(b => b >= 32 && b <= 126);
                if (!isValidName)
                {
                    vm.ReportError("Function name contains invalid characters", false);
                    return;
                }

                string functionName = System.Text.Encoding.UTF8.GetString(nameBytes);

                // Registrar todas las funciones en la tabla
                vm.FunctionTable[functionName] = funcStart;
                vm.OnDebugMessage($"Function registered: {functionName} at position {funcStart}");

                // Tratamiento especial para main
                if (functionName == "main")
                {
                    vm.OnDebugMessage($"Main function found with 0 parameters");
                    // Aquí podrías establecer una bandera que indique que se encontró main
                    vm.MainFound = true;
                }

                // Para todas las funciones, registrar sus parámetros también
                try
                {
                    // Leer número de parámetros
                    if (vm.Ip < vm.Bytecode.Count)
                    {
                        byte paramCount = vm.Bytecode[vm.Ip++];
                        vm.OnDebugMessage($"Function {functionName} has {paramCount} parameters");

                        // Saltar los nombres de los parámetros (no los necesitamos aquí)
                        for (int i = 0; i < paramCount && vm.Ip < vm.Bytecode.Count; i++)
                        {
                            byte paramNameLength = vm.Bytecode[vm.Ip++];
                            vm.Ip += paramNameLength;
                        }
                    }
                }
                catch (Exception ex)
                {
                    vm.OnDebugMessage($"Error reading parameters for function {functionName}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                vm.ReportError($"Error registering function: {ex.Message}", false);
            }
        }
    }
}