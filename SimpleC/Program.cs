using SimpleC.CodeGeneration;
using SimpleC.Lexing;
using SimpleC.Parsing;
using System;

namespace SimpleC
{
    class Program
    {
        static void Main(string[] args)
        {
            string code = @"
#include <stdio.h>
#include ""myheader.h""

int a = 5;

int func(int b)
{
    int c = (5*b)+7;
    return c;
}

int main()
{
    a = 6;
    func(4);

    return a*2;
}";

            //lexing

            var lexer = new Tokenizer(code);
            var tokens = lexer.Tokenize();

            foreach (var token in tokens)
            {
                Console.WriteLine($"  {token.Content}  ");
            }

            Console.WriteLine();
            Console.WriteLine();

            //parsing
            var parser = new Parser(tokens);
            var ast = parser.ParseToAst();

            // Code generation
            var emitter = new CodeEmitter();
            ast.EmitCode(emitter);

            // Obtener y mostrar el código de máquina generado
            var emittedCode = emitter.GetEmittedCode();
            using (var writer = new StreamWriter("output.asm"))
            {
                foreach (var instruction in emittedCode)
                {
                    writer.WriteLine($"{instruction.OpCode} {instruction.ByteArg1} {instruction.ByteArg2}");
                }
            }

            Console.ReadKey(false);
        }
    }
}
