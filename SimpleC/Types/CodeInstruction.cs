using System.Runtime.InteropServices;

namespace SimpleC.Types
{
    /// <summary>
    /// Instrucción única en el código de máquina de SimpleC.
    /// Consiste en un código de operación (opcode) de 8 bits y,
    /// ya sea un argumento de 16 bits o dos argumentos de 8 bits.
    /// Esta es una estructura que usa LayoutKind.Explicit.
    /// </summary>
    /// <remarks>
    /// Convención:
    /// Cuando se necesitan dos argumentos, ambos deben ser bytes.
    /// Si el único argumento es una dirección de memoria, es un short
    /// (para permitir direccionar toda la memoria). Cuando es un contador
    /// o una cantidad, es un byte.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    struct CodeInstruction
    {
        [FieldOffset(0)]
        public OpCode OpCode;
        [FieldOffset(1)]
        public byte ByteArg1;
        [FieldOffset(2)]
        public byte ByteArg2;
        [FieldOffset(1)] // Superposición sobre ByteArg1/2
        public short ShortArg;
    }
}
