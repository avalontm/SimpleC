namespace SimpleC.VM
{
    public class BytecodeProgram
    {
        public byte[] Bytecode { get; private set; }
        public object[] Constants { get; private set; }

        public BytecodeProgram(byte[] bytecode, object[] constants)
        {
            Bytecode = bytecode;
            Constants = constants;
        }
    }

}
