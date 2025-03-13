using System;
using System.Text;

namespace SimpleC.VM.Instructions
{
    /// <summary>
    /// Implementa operaciones aritméticas y de concatenación
    /// </summary>
    public static class ArithmeticOperations
    {
        public static void ExecuteAdd()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null)
            {
                throw new InvalidOperationException("VM instance not available");
            }

            if (vm.Stack.Count < 2)
            {
                vm.ReportError("Stack underflow: Add operation requires at least two operands");
                return;
            }

            // Obtener los operandos (en orden inverso)
            object operand2 = vm.Stack.Pop();
            object operand1 = vm.Stack.Pop();

            // Proteger contra valores nulos
            operand1 = operand1 ?? "";
            operand2 = operand2 ?? "";

            // Manejar diferentes tipos
            if (operand1 is string || operand2 is string)
            {
                // Convertir ambos a string si alguno es string (concatenación)
                string str1 = operand1.ToString();
                string str2 = operand2.ToString();
                vm.Stack.Push(str1 + str2);
                vm.OnDebugMessage($"String concatenation: \"{str1}\" + \"{str2}\" = \"{str1 + str2}\"");
            }
            else if (operand1 is int int1 && operand2 is int int2)
            {
                // Suma de enteros
                vm.Stack.Push(int1 + int2);
                vm.OnDebugMessage($"Integer addition: {int1} + {int2} = {int1 + int2}");
            }
            else if ((operand1 is int || operand1 is float) && (operand2 is int || operand2 is float))
            {
                // Suma de números con conversión implícita
                float result = Convert.ToSingle(operand1) + Convert.ToSingle(operand2);

                // Si ambos eran enteros originalmente, mantener el resultado como entero
                if (operand1 is int && operand2 is int)
                {
                    vm.Stack.Push((int)result);
                    vm.OnDebugMessage($"Integer addition: {operand1} + {operand2} = {(int)result}");
                }
                else
                {
                    vm.Stack.Push(result);
                    vm.OnDebugMessage($"Float addition: {operand1} + {operand2} = {result}");
                }
            }
            else
            {
                // Convertir a string como fallback
                string str1 = operand1.ToString();
                string str2 = operand2.ToString();
                vm.Stack.Push(str1 + str2);
                vm.OnDebugMessage($"Fallback string concatenation: \"{str1}\" + \"{str2}\" = \"{str1 + str2}\"");
            }
        }

        public static void ExecuteSubtract()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null)
            {
                throw new InvalidOperationException("VM instance not available");
            }

            if (vm.Stack.Count < 2)
            {
                vm.ReportError("Stack underflow: Subtract operation requires at least two operands");
                return;
            }

            object operand2 = vm.Stack.Pop();
            object operand1 = vm.Stack.Pop();

            // Proteger contra valores nulos
            if (operand1 == null || operand2 == null)
            {
                vm.ReportError("Cannot subtract null values");
                vm.Stack.Push(0); // Valor predeterminado en caso de error
                return;
            }

            if (operand1 is int int1 && operand2 is int int2)
            {
                vm.Stack.Push(int1 - int2);
                vm.OnDebugMessage($"Integer subtraction: {int1} - {int2} = {int1 - int2}");
            }
            else if ((operand1 is int || operand1 is float) && (operand2 is int || operand2 is float))
            {
                float result = Convert.ToSingle(operand1) - Convert.ToSingle(operand2);

                // Si ambos eran enteros originalmente, mantener el resultado como entero
                if (operand1 is int && operand2 is int)
                {
                    vm.Stack.Push((int)result);
                    vm.OnDebugMessage($"Integer subtraction: {operand1} - {operand2} = {(int)result}");
                }
                else
                {
                    vm.Stack.Push(result);
                    vm.OnDebugMessage($"Float subtraction: {operand1} - {operand2} = {result}");
                }
            }
            else
            {
                vm.ReportError($"Unsupported types for subtraction: {operand1.GetType().Name} and {operand2.GetType().Name}");
                vm.Stack.Push(0); // Valor predeterminado en caso de error
            }
        }

        public static void ExecuteMultiply()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null)
            {
                throw new InvalidOperationException("VM instance not available");
            }

            if (vm.Stack.Count < 2)
            {
                vm.ReportError("Stack underflow: Multiply operation requires at least two operands");
                return;
            }

            object operand2 = vm.Stack.Pop();
            object operand1 = vm.Stack.Pop();

            // Proteger contra valores nulos
            if (operand1 == null || operand2 == null)
            {
                vm.ReportError("Cannot multiply null values");
                vm.Stack.Push(0); // Valor predeterminado en caso de error
                return;
            }

            if (operand1 is int int1 && operand2 is int int2)
            {
                vm.Stack.Push(int1 * int2);
                vm.OnDebugMessage($"Integer multiplication: {int1} * {int2} = {int1 * int2}");
            }
            else if ((operand1 is int || operand1 is float) && (operand2 is int || operand2 is float))
            {
                float result = Convert.ToSingle(operand1) * Convert.ToSingle(operand2);

                // Si ambos eran enteros originalmente, mantener el resultado como entero
                if (operand1 is int && operand2 is int)
                {
                    vm.Stack.Push((int)result);
                    vm.OnDebugMessage($"Integer multiplication: {operand1} * {operand2} = {(int)result}");
                }
                else
                {
                    vm.Stack.Push(result);
                    vm.OnDebugMessage($"Float multiplication: {operand1} * {operand2} = {result}");
                }
            }
            else if (operand1 is string str && operand2 is int count)
            {
                // Multiplicación de string: repetir n veces
                if (count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < count; i++)
                    {
                        sb.Append(str);
                    }
                    string result = sb.ToString();
                    vm.Stack.Push(result);
                    vm.OnDebugMessage($"String repetition: \"{str}\" * {count} = \"{result}\"");
                }
                else
                {
                    vm.Stack.Push("");
                    vm.OnDebugMessage($"String repetition: \"{str}\" * {count} = \"\"");
                }
            }
            else if (operand2 is string str2 && operand1 is int count2)
            {
                // Multiplicación de string (orden inverso)
                if (count2 > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < count2; i++)
                    {
                        sb.Append(str2);
                    }
                    string result = sb.ToString();
                    vm.Stack.Push(result);
                    vm.OnDebugMessage($"String repetition: {count2} * \"{str2}\" = \"{result}\"");
                }
                else
                {
                    vm.Stack.Push("");
                    vm.OnDebugMessage($"String repetition: {count2} * \"{str2}\" = \"\"");
                }
            }
            else
            {
                vm.ReportError($"Unsupported types for multiplication: {operand1.GetType().Name} and {operand2.GetType().Name}");
                vm.Stack.Push(0); // Valor predeterminado en caso de error
            }
        }

        public static void ExecuteDivide()
        {
            var vm = VirtualMachine.Instance;
            if (vm == null)
            {
                throw new InvalidOperationException("VM instance not available");
            }

            if (vm.Stack.Count < 2)
            {
                vm.ReportError("Stack underflow: Divide operation requires at least two operands");
                return;
            }

            object operand2 = vm.Stack.Pop();
            object operand1 = vm.Stack.Pop();

            // Proteger contra valores nulos
            if (operand1 == null || operand2 == null)
            {
                vm.ReportError("Cannot divide null values");
                vm.Stack.Push(0); // Valor predeterminado en caso de error
                return;
            }

            // Comprobar divisiones por cero
            if ((operand2 is int i2 && i2 == 0) ||
                (operand2 is float f2 && f2 == 0) ||
                (operand2 is double d2 && d2 == 0))
            {
                vm.ReportError("Division by zero");
                vm.Stack.Push(float.PositiveInfinity); // Indicar división por cero
                return;
            }

            if (operand1 is int int1 && operand2 is int int2)
            {
                // En lenguajes como C, la división de enteros trunca hacia cero
                vm.Stack.Push(int1 / int2);
                vm.OnDebugMessage($"Integer division: {int1} / {int2} = {int1 / int2}");
            }
            else if ((operand1 is int || operand1 is float) && (operand2 is int || operand2 is float))
            {
                float result = Convert.ToSingle(operand1) / Convert.ToSingle(operand2);
                vm.Stack.Push(result);
                vm.OnDebugMessage($"Float division: {operand1} / {operand2} = {result}");
            }
            else
            {
                vm.ReportError($"Unsupported types for division: {operand1.GetType().Name} and {operand2.GetType().Name}");
                vm.Stack.Push(0); // Valor predeterminado en caso de error
            }
        }
    }
}