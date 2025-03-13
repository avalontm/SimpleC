using System.Diagnostics;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa la instrucción para retornar de una función
    /// </summary>
    public static class ProcessReturn
    {
        public static void Execute()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null) return;

            // Avanzar después del opcode Return
            vm.Ip++;

            // Verificar si tenemos un valor para retornar
            object returnValue = null;
            if (vm.Stack.Count > 0)
            {
                returnValue = vm.Stack.Peek(); // IMPORTANTE: Solo mirar el valor, no sacarlo de la pila
            }


            if (vm.CallStack.Count > 0)
            {
                // Eliminar contexto local
                if (vm.LocalContexts.Count > 0)
                {
                    vm.LocalContexts.Pop();
                }

                // Obtener dirección de retorno
                int returnAddress = vm.CallStack.Pop();

                // Restaurar posición
                vm.Ip = returnAddress;
            }

        }
    }
}