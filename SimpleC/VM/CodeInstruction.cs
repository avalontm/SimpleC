using System.Runtime.InteropServices;

namespace SimpleC.VM
{
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
