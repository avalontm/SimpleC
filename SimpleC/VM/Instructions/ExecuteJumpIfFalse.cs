using System.Text;

namespace SimpleC.VM.Instructions
{
    public static class JumpInstructions
    {
        public static void ExecuteJumpIfFalse()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null) return;

            int startIp = vm.Ip;
            try
            {
                vm.Ip++;

                // Leer la longitud del bloque del if
                byte[] ifBlockLengthBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    ifBlockLengthBytes[i] = vm.Bytecode[vm.Ip + i];
                }
                int ifBlockLength = BitConverter.ToInt32(ifBlockLengthBytes, 0);
                vm.Ip += 4;

                // Verificar si hay suficientes valores en la pila para la condición
                if (vm.Stack.Count == 0)
                {
                    vm.ReportError("Stack underflow during conditional jump");
                    return;
                }

                // Obtener y sacar el valor de la condición de la pila
                object conditionValue = vm.Stack.Pop();

                // Evaluar la condición
                bool conditionResult = EvaluateCondition(conditionValue);

                vm.OnDebugMessage($"Condition: {conditionValue} => {conditionResult}");

                if (conditionResult)
                {
                    // Si la condición es verdadera, ejecutar el bloque del if
                    vm.OnDebugMessage($"Executing IF block");
                    // El bloque del if ya está listo para ser ejecutado, no necesitamos saltar
                }
                else
                {
                    // Si la condición es falsa, saltar el bloque del if 
                    // y prepararse para ejecutar el bloque del else
                    vm.OnDebugMessage($"Skipping IF block, preparing to execute ELSE block");
                    vm.Ip += ifBlockLength;
                }
            }
            catch (Exception ex)
            {
                vm.OnDebugMessage($"Error in JumpIfFalse at IP {startIp}: {ex.Message}");
                throw;
            }
        }

        public static void ExecuteJump()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null) return;

            int startIp = vm.Ip;
            try
            {
                // Avanzar después del opcode Jump
                vm.Ip++;

                // Leer la longitud del bloque a saltar
                byte[] lengthBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    lengthBytes[i] = vm.Bytecode[vm.Ip + i];
                }
                int blockLength = BitConverter.ToInt32(lengthBytes, 0);

                vm.OnDebugMessage($"Unconditional jump of {blockLength} bytes");

                // Saltar el bloque
                vm.Ip += 4 + blockLength;
            }
            catch (Exception ex)
            {
                vm.OnDebugMessage($"Error in Jump at IP {startIp}: {ex.Message}");
                throw;
            }
        }

        private static bool EvaluateCondition(object conditionValue)
        {
            switch (conditionValue)
            {
                case bool boolValue:
                    return boolValue;
                case int intValue:
                    return intValue != 0;
                case double doubleValue:
                    return doubleValue != 0;
                case float floatValue:
                    return floatValue != 0;
                case string stringValue:
                    return !string.IsNullOrEmpty(stringValue);
                case char charValue:
                    return charValue != '\0';
                case null:
                    return false;
                default:
                    // Para otros tipos, considerar no nulo como verdadero
                    return true;
            }
        }
    }
}