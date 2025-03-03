using SimpleC.Types;

namespace SimpleC.Excecution
{
    /// <summary>
    /// Ejecutador para código de máquina de SimpleC.
    /// </summary>
    class Excecutor
    {
        private const int DEFAULT_MEMORY_SIZE = 1000; // 1k Celdas = 4kB de Memoria

        private int[] memory; // Una celda de memoria tiene el tamaño de un entero (sizeof(int))
        private int programCounter;
        private int stackPointer;
        private int heapPointer;
        private int framePointer;
        private int extremePointer; // TODO: ¿Es necesario?
        private CodeInstruction[] code;

        public Excecutor(CodeInstruction[] code)
            : this(code, 0)
        { }

        public Excecutor(CodeInstruction[] code, int memorySize)
        {
            if (memorySize < 0)
                throw new ArgumentOutOfRangeException("Se proporcionó un tamaño de memoria negativo", "memorySize");

            if (memorySize > short.MaxValue)
                throw new ArgumentOutOfRangeException("El tamaño de la memoria es demasiado grande (¡direcciones de 16 bits!)", "memorySize");

            if (memorySize == 0) // Automático
                memorySize = DEFAULT_MEMORY_SIZE;

            memory = new int[memorySize];

            this.code = code;
        }

        /// <summary>
        /// Inicia la ejecución del código.
        /// </summary>
        public void Start()
        {
            programCounter = 0;
            stackPointer = 0;
            heapPointer = memory.Length - 1;
            framePointer = 0;
            extremePointer = 0;

            CodeInstruction instrunction = code[0];

            while (instrunction.OpCode != OpCode.Halt)
            {
                exceuteInstruction(instrunction);
                programCounter++;
            }
        }

        private void exceuteInstruction(CodeInstruction instr)
        {
            // Generalmente: No se realiza mucha verificación de operandos aquí (como en un hardware real).
            // Si algunos argumentos son incorrectos, el comportamiento no está definido.
            switch (instr.OpCode)
            {
                case OpCode.LoadC:
                    stackPointer++;
                    memory[stackPointer] = instr.ShortArg;
                    break;
                case OpCode.Load:
                    for (int i = instr.ByteArg1 - 1; i >= 0; i--)
                        memory[stackPointer + i] = memory[memory[stackPointer] + i];
                    stackPointer += instr.ByteArg1 - 1;
                    break;
                case OpCode.LoadA:
                    stackPointer++;
                    for (int i = instr.ByteArg2 - 1; i >= 0; i--)
                        memory[stackPointer + i] = memory[instr.ByteArg1 + i];
                    stackPointer += instr.ByteArg2 - 1;
                    break;
                case OpCode.Dup:
                    memory[stackPointer + 1] = memory[stackPointer];
                    stackPointer++;
                    break;
                case OpCode.LoadRc:
                    stackPointer++;
                    memory[stackPointer] = framePointer + instr.ShortArg;
                    break;
                case OpCode.LoadR:
                    stackPointer++;
                    for (int i = instr.ByteArg2 - 1; i >= 0; i--)
                        memory[stackPointer + i] = memory[framePointer + instr.ByteArg1 + i];
                    stackPointer += instr.ByteArg2 - 1;
                    break;
                case OpCode.LoadMc:
                    stackPointer++;
                    memory[stackPointer] = memory[framePointer - 3] + instr.ByteArg1;
                    break;
                case OpCode.LoadM:
                    stackPointer++;
                    for (int i = instr.ByteArg2 - 1; i >= 0; i--)
                        memory[stackPointer + i] = memory[memory[framePointer - 3] + instr.ByteArg1];
                    stackPointer += instr.ByteArg2 - 1;
                    break;
                case OpCode.LoadV:
                    memory[stackPointer + 1] = memory[memory[memory[stackPointer - 2]] + instr.ByteArg1];
                    stackPointer++;
                    break;
                case OpCode.LoadSc:
                    memory[stackPointer + 1] = stackPointer - instr.ByteArg1;
                    stackPointer++;
                    break;
                case OpCode.LoadS:
                    stackPointer++;
                    memory[stackPointer] = memory[stackPointer - instr.ByteArg1];
                    break;
                case OpCode.Pop:
                    stackPointer -= instr.ByteArg1;
                    break;
                case OpCode.Store:
                    for (int i = 0; i < instr.ByteArg1; i++)
                        memory[memory[stackPointer] + i] = memory[stackPointer - instr.ByteArg1 + i];
                    stackPointer--;
                    break;
                case OpCode.StoreA:
                    stackPointer++;
                    for (int i = 0; i < instr.ByteArg2; i++)
                        memory[instr.ByteArg1 + i] = memory[stackPointer - instr.ByteArg2 + i];
                    stackPointer--;
                    break;
                case OpCode.StoreR:
                    stackPointer++;
                    for (int i = 0; i < instr.ByteArg2; i++)
                        memory[framePointer + instr.ByteArg1 + i] = memory[stackPointer - instr.ByteArg2 + i];
                    stackPointer--;
                    break;
                case OpCode.StoreM:
                    stackPointer++;
                    for (int i = 0; i < instr.ByteArg2; i++)
                        memory[memory[framePointer - 3] + instr.ByteArg1 + i] = memory[stackPointer - instr.ByteArg2 + i];
                    stackPointer--;
                    break;
                case OpCode.Jump:
                    programCounter = instr.ShortArg;
                    break;
                case OpCode.JumpZ:
                    if (memory[stackPointer] == 0)
                        programCounter = instr.ShortArg;
                    stackPointer--;
                    break;
                case OpCode.JumpI:
                    programCounter = instr.ShortArg + memory[stackPointer];
                    stackPointer--;
                    break;
                case OpCode.Add:
                    memory[stackPointer - 1] = memory[stackPointer - 1] + memory[stackPointer];
                    stackPointer--;
                    break;
                case OpCode.Sub:
                    memory[stackPointer - 1] = memory[stackPointer - 1] - memory[stackPointer];
                    stackPointer--;
                    break;
                case OpCode.Mul:
                    memory[stackPointer - 1] = memory[stackPointer - 1] * memory[stackPointer];
                    stackPointer--;
                    break;
                case OpCode.Div:
                    memory[stackPointer - 1] = memory[stackPointer - 1] / memory[stackPointer];
                    stackPointer--;
                    break;
                case OpCode.Mod:
                    memory[stackPointer - 1] = memory[stackPointer - 1] % memory[stackPointer];
                    stackPointer--;
                    break;
                case OpCode.Neg:
                    memory[stackPointer] = -memory[stackPointer];
                    break;
                case OpCode.Eq:
                    memory[stackPointer - 1] = (memory[stackPointer - 1] == memory[stackPointer]) ? 1 : 0;
                    stackPointer--;
                    break;
                case OpCode.Mark:
                    memory[stackPointer + 1] = extremePointer;
                    memory[stackPointer + 2] = framePointer;
                    stackPointer += 2;
                    break;
                case OpCode.Call:
                    framePointer = stackPointer;
                    int tmp = programCounter;
                    programCounter = memory[stackPointer];
                    memory[stackPointer] = tmp;
                    break;
                case OpCode.Return:
                    programCounter = memory[framePointer];
                    extremePointer = memory[framePointer - 2];
                    if (extremePointer >= heapPointer)
                        throw new StackOverflowException();
                    stackPointer = framePointer - instr.ByteArg1;
                    framePointer = memory[framePointer - 1];
                    break;
                case OpCode.New:
                    if (heapPointer - memory[stackPointer] > extremePointer)
                    {
                        heapPointer = heapPointer - memory[stackPointer];
                        memory[stackPointer] = heapPointer;
                    }
                    else
                        memory[stackPointer] = 0; // Sin memoria disponible
                    break;
                case OpCode.Nop:
                    // No hacer nada...
                    break;
                case OpCode.Halt:
                    // No hacer nada, será manejado por el ciclo de ejecución principal
                    break;
                default:
                    throw new InvalidOpCodeException("Se encontró un código de operación desconocido. OpCode: " + instr.OpCode);
            }
        }
    }
}
