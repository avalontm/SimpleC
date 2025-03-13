using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para registrar una función en la tabla de funciones
    /// </summary>
    public static class RegisterFunction
    {
        /// <summary>
        /// Ejecuta el proceso de registrar una función durante el escaneo del bytecode
        /// </summary>
        public static void Execute()
        {
            int funcStart = VirtualMachine.Instance.Ip; // Guardar posición Mark
            VirtualMachine.Instance.Ip++; // Avanzar a longitud de nombre

            if (VirtualMachine.Instance.Ip >= VirtualMachine.Instance.Bytecode.Count)
            {
                VirtualMachine.Instance.ReportError("Unexpected end of bytecode while reading function name length", false);
                return;
            }

            byte nameLength = VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip];
            VirtualMachine.Instance.Ip++; // Avanzar al nombre

            if (VirtualMachine.Instance.Ip + nameLength <= VirtualMachine.Instance.Bytecode.Count)
            {
                string functionName = Encoding.UTF8.GetString(VirtualMachine.Instance.Bytecode.ToArray(), VirtualMachine.Instance.Ip, nameLength);

                // Registrar función con posición de Mark
                VirtualMachine.Instance.FunctionTable[functionName] = funcStart;
                VirtualMachine.Instance.OnDebugMessage($"Function registered: {functionName} at position {funcStart}");

                // Avanzar más allá del nombre
                VirtualMachine.Instance.Ip += nameLength;

                // Saltar el cuerpo de la función para continuar escaneo
                SkipFunctionBody();
            }
            else
            {
                VirtualMachine.Instance.OnDebugMessage("Error reading function name");
                VirtualMachine.Instance.Ip++;
            }
        }

        /// <summary>
        /// Salta el cuerpo de una función durante el escaneo de bytecode
        /// </summary>
        private static void SkipFunctionBody()
        {
            // Estamos después del nombre de la función

            // Saltar más allá de OpCode.Enter
            if (VirtualMachine.Instance.Ip < VirtualMachine.Instance.Bytecode.Count &&
                (OpCode)VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip] == OpCode.Enter)
                VirtualMachine.Instance.Ip++;

            // Saltar más allá del tipo de retorno
            if (VirtualMachine.Instance.Ip < VirtualMachine.Instance.Bytecode.Count)
                VirtualMachine.Instance.Ip++;

            // Leer número de parámetros
            if (VirtualMachine.Instance.Ip >= VirtualMachine.Instance.Bytecode.Count)
                return;

            byte paramCount = VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip++];

            // Saltar parámetros
            for (int i = 0; i < paramCount; i++)
            {
                // Saltar tipo de parámetro
                if (VirtualMachine.Instance.Ip < VirtualMachine.Instance.Bytecode.Count)
                    VirtualMachine.Instance.Ip++;

                // Saltar nombre de parámetro
                if (VirtualMachine.Instance.Ip < VirtualMachine.Instance.Bytecode.Count)
                {
                    byte paramNameLength = VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip++];
                    if (VirtualMachine.Instance.Ip + paramNameLength <= VirtualMachine.Instance.Bytecode.Count)
                        VirtualMachine.Instance.Ip += paramNameLength;
                }
            }

            // Saltar al siguiente Mark o hasta encontrar un Return
            int nestedLevel = 1; // Ya estamos dentro de una función

            while (VirtualMachine.Instance.Ip < VirtualMachine.Instance.Bytecode.Count)
            {
                OpCode currentOp = (OpCode)VirtualMachine.Instance.Bytecode[VirtualMachine.Instance.Ip];

                if (currentOp == OpCode.Mark || currentOp == OpCode.Enter)
                    nestedLevel++;
                else if (currentOp == OpCode.Return)
                {
                    nestedLevel--;
                    if (nestedLevel == 0)
                    {
                        VirtualMachine.Instance.Ip++; // Avanzar más allá del Return
                        break;
                    }
                }

                VirtualMachine.Instance.Ip++;
            }
        }
    }
}