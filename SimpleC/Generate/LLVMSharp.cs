using LLVMSharp.Interop;
using SimpleC.Types.Tokens;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleC
{
    public static unsafe class LLVMSharp
    {
        static string outputLlvmFile = "output.ll";
        static string outputExeFile = "output.exe";
        static LLVMModuleRef module;
        static LLVMBuilderRef builder;
        static LLVMContextRef context;
        static LLVMBasicBlockRef entry;

        // Diccionario de variables
        static Dictionary<string, LLVMValueRef> variables = new Dictionary<string, LLVMValueRef>();
        // Diccionario para almacenar las variables globales
        static Dictionary<string, LLVMValueRef> globalVariables = new Dictionary<string, LLVMValueRef>();

        // Diccionario que almacena los tipos de las variables declaradas
        private static Dictionary<string, VariableType> variableTypes = new Dictionary<string, VariableType>();

        static unsafe sbyte* SByteConvert(string name)
        {
            return (sbyte*)Marshal.StringToHGlobalAnsi(name).ToPointer();
        }

        public static void Init()
        {
            // Crear el módulo LLVM
            module = LLVM.ModuleCreateWithName(SByteConvert("main_module"));
            builder = LLVM.CreateBuilder();
            context = LLVM.ContextCreate();

            LLVM.SetTarget(module, SByteConvert("x86_64-pc-windows-msvc19.42.34436"));
        }

        public static void Declare(string name)
        {
            // Declarar la función printf
            var functionType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, new LLVMTypeRef[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) });
            LLVM.AddFunction(module, SByteConvert(name), functionType);
        }

        public static void Print(string str)
        {
            LLVMValueRef formatString = null;

            // Caso 1: Si el parámetro es una cadena literal (string)
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                // Eliminar las comillas al inicio y al final
                str = str.Substring(1, str.Length - 2);

                // Crear la cadena global sin las comillas
                formatString = LLVM.BuildGlobalStringPtr(builder, SByteConvert($"{str}\n"), SByteConvert("formatString"));

                // Llamar a printf con la cadena literal
                LLVMTypeRef printfType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, new LLVMTypeRef[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }, true);
                LLVMValueRef[] printfArgs = new LLVMValueRef[] { formatString };
                builder.BuildCall2(printfType, LLVM.GetNamedFunction(module, SByteConvert("printf")), printfArgs, "call");
            }
            else
            {
                LLVMValueRef variableValue = null;

                try
                {
                    variableValue = variables[str];
                }
                catch
                {
                    try
                    {
                        variableValue = globalVariables[str];
                    }
                    catch
                    {
                        variableValue = null;
                    }
                }

                // Caso 2: Si el parámetro es una variable (int, float, bool, string, etc.)
                if (variableValue.Handle != IntPtr.Zero)
                {
                    // Determinar el tipo de la variable
                    VariableType varType = variableTypes[str];

                    LLVMTypeRef llvmType = GetLLVMType(varType); // Obtener el tipo LLVM
                    LLVMValueRef value;

                    // Si la variable es un puntero a string (char* o string en general)
                    if (llvmType == LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0))
                    {
                        // Obtener el valor apuntado por el puntero (la cadena)
                        value = LLVM.BuildLoad2(builder, llvmType, variableValue, SByteConvert("variableValue"));

                        // Usar %s para imprimir la cadena
                        formatString = LLVM.BuildGlobalStringPtr(builder, SByteConvert("%s\n"), SByteConvert("formatString"));
                        LLVMTypeRef printfType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, new LLVMTypeRef[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }, true);
                        LLVMValueRef[] printfArgs = new LLVMValueRef[] { formatString, value };
                        builder.BuildCall2(printfType, LLVM.GetNamedFunction(module, SByteConvert("printf")), printfArgs, "call");
                    }
                    // Otros tipos (int, float, bool, etc.)
                    else
                    {
                        value = LLVM.BuildLoad2(builder, llvmType, variableValue, SByteConvert("variableValue"));
                        if (llvmType == LLVMTypeRef.Int32)
                        {
                            formatString = LLVM.BuildGlobalStringPtr(builder, SByteConvert("%d\n"), SByteConvert("formatString"));
                        }
                        else if (llvmType == LLVMTypeRef.Float)
                        {
                            formatString = LLVM.BuildGlobalStringPtr(builder, SByteConvert("%f\n"), SByteConvert("formatString"));
                        }
                        else if (llvmType == LLVMTypeRef.Int1)
                        {
                            formatString = LLVM.BuildGlobalStringPtr(builder, SByteConvert("%d\n"), SByteConvert("formatString"));
                        }
                        else if (llvmType == LLVMTypeRef.Int8)
                        {
                            formatString = LLVM.BuildGlobalStringPtr(builder, SByteConvert("%c\n"), SByteConvert("formatString"));
                        }

                        LLVMTypeRef printfType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, new LLVMTypeRef[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) }, true);
                        LLVMValueRef[] printfArgs = new LLVMValueRef[] { formatString, value };
                        builder.BuildCall2(printfType, LLVM.GetNamedFunction(module, SByteConvert("printf")), printfArgs, "call");
                    }
                }
                else
                {
                    throw new Exception($"Error: La variable '{str}' no está definida.");
                }
            }
        }


        public static void Method(VariableType type, string name)
        {
            // Verificar el tipo y crear el tipo de función adecuado
            LLVMTypeRef returnType;
            LLVMTypeRef[] paramTypes;

            // Verificar el tipo de retorno
            switch (type)
            {
                case VariableType.Int:
                    returnType = LLVMTypeRef.Int32; // Si el tipo es int, el tipo de retorno será i32
                    break;
                case VariableType.Float:
                    returnType = LLVMTypeRef.Float; // Si el tipo es float, el tipo de retorno será float
                    break;
                case VariableType.Bool:
                    returnType = LLVMTypeRef.Int1; // Si el tipo es bool, el tipo de retorno será i1
                    break;
                case VariableType.String:
                    returnType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0); // Si el tipo es string, el tipo de retorno será un puntero a i8
                    break;
                case VariableType.Void:
                    returnType = LLVMTypeRef.Void; // Si el tipo es Void, usamos LLVMTypeRef.Void
                    break;
                case VariableType.Char:
                    returnType = LLVMTypeRef.Int8; // Si el tipo es Char, usamos i8 para representar caracteres
                    break;
                default:
                    throw new Exception($"Error: Tipo de retorno no soportado: {type}");
            }

            // Verificar los parámetros de la función, puedes ajustar esto según el tipo de parámetros
            if (name.Contains("main"))
            {
                // Para la función 'main', se espera que los parámetros sean Int32 y un puntero a char (char[])
                paramTypes = new LLVMTypeRef[] { LLVMTypeRef.Int32, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) };
            }
            else
            {
                // Aquí puedes agregar otros tipos de parámetros si es necesario
                paramTypes = new LLVMTypeRef[] { returnType }; // Ejemplo simple con solo un parámetro del tipo de retorno
            }

            // Crear el tipo de la función
            var functionType = LLVMTypeRef.CreateFunction(returnType, paramTypes);

            // Añadir la función al módulo
            var function = LLVM.AddFunction(module, SByteConvert(name), functionType);

            if (name.Contains("main"))
            {
                // Si es la función main, se agrega el bloque básico de entrada
                entry = LLVM.AppendBasicBlock(function, SByteConvert("entry"));
                LLVM.PositionBuilderAtEnd(builder, entry);
            }
        }


        public static void Variable(VariableType type, string name, string value, bool IsGlobal = false)
        {
            // Verificar si la variable es global
            if (IsGlobal)
            {
                // Si es global, asegurarse de que esté declarada en LLVM
                LLVMValueRef globalVar = DeclareGlobalVariable(type, name, value);
                variableTypes[name] = type;

            }
            else
            {
                // Verificar que el builder esté inicializado
                if (builder.Handle == IntPtr.Zero)
                {
                    throw new Exception("Error: El builder no está inicializado.");
                }

                // Verificar que el tipo LLVM sea válido
                LLVMTypeRef llvmType = GetLLVMType(type);
                if (llvmType.Handle == IntPtr.Zero)
                {
                    throw new Exception("Error: El tipo LLVM no es válido.");
                }

                // Verificar que el nombre no sea nulo o vacío
                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception("Error: El nombre de la variable no puede ser nulo o vacío.");
                }

                // Convertir el nombre a sbyte* y liberar la memoria después de su uso
                sbyte* namePtr = SByteConvert(name);
                try
                {
                    // Crear la variable en la pila
                    var variable = LLVM.BuildAlloca(builder, llvmType, namePtr);

                    // Guardar la variable en un diccionario para futuras referencias
                    variables[name] = variable;
                    variableTypes[name] = type;

                    // Si hay un valor, evaluar si es una expresión
                    if (!string.IsNullOrEmpty(value))
                    {
                        LLVMValueRef llvmValue = ParseExpression(name, value, type);

                        // Almacenar el valor evaluado en la variable
                        LLVM.BuildStore(builder, llvmValue, variable);
                    }
                }
                finally
                {
                    // Liberar la memoria asignada para el nombre
                    Marshal.FreeHGlobal((IntPtr)namePtr);
                }
            }
        }

        private static LLVMValueRef DeclareGlobalVariable(VariableType type, string name, string value)
        {
            LLVMTypeRef llvmType = GetLLVMType(type);

            // Verificar si la variable global ya existe en el módulo, para evitar duplicados
            LLVMValueRef existingGlobal = LLVM.GetNamedGlobal(module, SByteConvert(name));

            if (existingGlobal.Handle != IntPtr.Zero)
            {
                throw new Exception($"Error: La variable global '{name}' ya está declarada.");
            }

            // Declarar la variable global
            LLVMValueRef globalVar = LLVM.AddGlobal(module, llvmType, SByteConvert(name));
            Debug.WriteLine($"Register: {name}");
            globalVariables[name] = globalVar;

            // Si hay un valor, establecer el inicializador
            if (!string.IsNullOrEmpty(value))
            {
                LLVMValueRef llvmValue = ParseExpression(name, value, type);
                LLVM.SetInitializer(globalVar, llvmValue);
            }
            else
            {
                // Si no se proporciona valor, inicializarla con un valor por defecto
                if (type == VariableType.Int)
                {
                    LLVM.SetInitializer(globalVar, LLVM.ConstInt(LLVMTypeRef.Int32, 0, 1));
                }
                else if (type == VariableType.Float)
                {
                    LLVM.SetInitializer(globalVar, LLVM.ConstReal(LLVMTypeRef.Float, 0.0));
                }
                else if (type == VariableType.Bool)
                {
                    LLVM.SetInitializer(globalVar, LLVM.ConstInt(LLVMTypeRef.Int1, 0, 0));
                }
                else if (type == VariableType.String)
                {
                    // Para el tipo string, inicializar con una cadena vacía o un valor por defecto.
                    // Usamos LLVM.BuildGlobalStringPtr para crear una cadena global.
                    // No convertimos a `sbyte*` aquí, simplemente pasamos el string directamente.
                    LLVMValueRef stringLLVMValue = LLVM.BuildGlobalStringPtr(builder, SByteConvert("empty_string"), SByteConvert("global_string"));
                    LLVM.SetInitializer(globalVar, stringLLVMValue);
                }
                // Agregar otros tipos según sea necesario
            }

            // Asegurarse de que el linkage sea el correcto para una variable global
            LLVM.SetLinkage(globalVar, LLVMLinkage.LLVMExternalLinkage);

            return globalVar;
        }

        private static LLVMValueRef ParseExpression(string name, string expr, VariableType expectedType)
        {
            expr = expr.Trim();

            // Es un número entero
            if (int.TryParse(expr, out int numValue) && expectedType == VariableType.Int)
                return LLVM.ConstInt(LLVMTypeRef.Int32, (ulong)numValue, 1);

            // Es un número flotante
            if (double.TryParse(expr, NumberStyles.Float, CultureInfo.InvariantCulture, out double floatValue) && expectedType == VariableType.Float)
                return LLVM.ConstReal(LLVMTypeRef.Float, floatValue);

            // Es una variable existente
            if (variables.TryGetValue(expr, out LLVMValueRef variableRef))
            {
                VariableType varType = variableTypes[expr];

                if (IsGlobalVariable(name))
                    return LLVM.BuildLoad2(builder, GetLLVMType(varType), variableRef, SByteConvert(expr));

                return LLVM.BuildLoad2(builder, GetLLVMType(varType), variableRef, SByteConvert(expr));
            }

            // Es un booleano
            if ((expr == "true" || expr == "false") && expectedType == VariableType.Bool)
                return LLVM.ConstInt(LLVMTypeRef.Int1, expr == "true" ? 1UL : 0UL, 0);

            // Es una cadena de texto
            if (expectedType == VariableType.String && expr.StartsWith("\"") && expr.EndsWith("\""))
            {
                string stringValue = expr.Substring(1, expr.Length - 2); // Eliminar comillas

                uint length = (uint)Encoding.ASCII.GetByteCount(stringValue) + 1; // +1 para `\0`

                // Para variables globales
                if (IsGlobalVariable(name))
                {
                    LLVMTypeRef arrayType = LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, length);
                    LLVMValueRef globalVar = LLVM.AddGlobal(module, arrayType, SByteConvert(name));
                    LLVMValueRef constStr = LLVM.ConstString(SByteConvert(stringValue), length, 1);
                    LLVM.SetInitializer(globalVar, constStr);
                    LLVM.SetLinkage(globalVar, LLVMLinkage.LLVMPrivateLinkage);
                    return globalVar;
                }

                // Para variables locales
                LLVMValueRef alloca = LLVM.BuildAlloca(builder, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), SByteConvert(name));
                LLVMValueRef stringLLVMValue = LLVM.BuildGlobalStringPtr(builder, SByteConvert(stringValue), SByteConvert("local_string"));
                LLVM.BuildStore(builder, stringLLVMValue, alloca);
                return alloca;
            }

            return ParseMathExpression(name, expr, expectedType);
        }



        // Método para verificar si una variable es global
        private static bool IsGlobalVariable(string name)
        {
            return globalVariables.ContainsKey(name);  // globalVariables es un diccionario que contiene las variables globales.
        }

        private static LLVMValueRef ParseMathExpression(string name, string expr, VariableType expectedType)
        {
            if (expectedType == VariableType.Char)
            {
                return null;
            }
            expr = expr.Replace(" ", ""); // Eliminar espacios en blanco innecesarios

            // Caso 1: Si la expresión está entre paréntesis, procesar recursivamente el contenido
            var parenMatch = Regex.Match(expr, @"^\((.*)\)$");
            if (parenMatch.Success)
            {
                // Recursivamente evaluar la expresión dentro de los paréntesis
                return ParseMathExpression(name, parenMatch.Groups[1].Value, expectedType);
            }

            // Caso 2: Intentar detectar una expresión básica con un operador
            var match = Regex.Match(expr, @"^(\d+|\w+)([\+\-\*/\^])(\d+|\w+)$");
            if (match.Success)
            {
                string left = match.Groups[1].Value;
                string op = match.Groups[2].Value;
                string right = match.Groups[3].Value;

                LLVMValueRef leftValue = ParseExpression(name, left, expectedType);
                LLVMValueRef rightValue = ParseExpression(name, right, expectedType);

                // Verificar si el tipo esperado es 'char' y ajustarlo a 'i8' para operaciones
                if (expectedType == VariableType.Char)
                {
                    leftValue = LLVM.BuildSExt(builder, leftValue, LLVMTypeRef.Int8, SByteConvert("char_left"));
                    rightValue = LLVM.BuildSExt(builder, rightValue, LLVMTypeRef.Int8, SByteConvert("char_right"));
                }

                switch (op)
                {
                    case "+":
                        return LLVM.BuildAdd(builder, leftValue, rightValue, SByteConvert("sum_tmp"));
                    case "-":
                        return LLVM.BuildSub(builder, leftValue, rightValue, SByteConvert("sub_tmp"));
                    case "*":
                        return LLVM.BuildMul(builder, leftValue, rightValue, SByteConvert("mul_tmp"));
                    case "/":
                        return LLVM.BuildUDiv(builder, leftValue, rightValue, SByteConvert("div_tmp"));
                    case "^":
                        // Implementar la exponenciación usando la función pow o algún otro método
                        return Exponentiate(leftValue, rightValue);
                    default:
                        throw new Exception($"Operador '{op}' no soportado.");
                }
            }

            // Caso 3: Si no coincide con un patrón simple, intentar expresiones más complejas
            var complexMatch = Regex.Match(expr, @"^(\d+|\w+)([\+\-\*/\^])(.+)$");
            if (complexMatch.Success)
            {
                string left = complexMatch.Groups[1].Value;
                string op = complexMatch.Groups[2].Value;
                string right = complexMatch.Groups[3].Value;

                // Evaluar recursivamente la expresión más compleja
                LLVMValueRef leftValue = ParseExpression(name, left, expectedType);
                LLVMValueRef rightValue = ParseMathExpression(name, right, expectedType); // Procesar la sub-expresión a la derecha

                // Verificar si el tipo esperado es 'char' y ajustarlo a 'i8' para operaciones
                if (expectedType == VariableType.Char)
                {
                    leftValue = LLVM.BuildSExt(builder, leftValue, LLVMTypeRef.Int8, SByteConvert("char_left"));
                    rightValue = LLVM.BuildSExt(builder, rightValue, LLVMTypeRef.Int8, SByteConvert("char_right"));
                }

                switch (op)
                {
                    case "+":
                        return LLVM.BuildAdd(builder, leftValue, rightValue, SByteConvert("sum_tmp"));
                    case "-":
                        return LLVM.BuildSub(builder, leftValue, rightValue, SByteConvert("sub_tmp"));
                    case "*":
                        return LLVM.BuildMul(builder, leftValue, rightValue, SByteConvert("mul_tmp"));
                    case "/":
                        return LLVM.BuildUDiv(builder, leftValue, rightValue, SByteConvert("div_tmp"));
                    case "^":
                        return Exponentiate(leftValue, rightValue);
                    default:
                        throw new Exception($"Operador '{op}' no soportado.");
                }
            }

            // Si no se reconoce la expresión, lanzar una excepción
            throw new Exception($"Error: Token no reconocido en expresión: '{expr}'.");
        }


        private static LLVMValueRef Exponentiate(LLVMValueRef baseValue, LLVMValueRef exponentValue)
        {
            // Comprobar si el tipo de los valores es el esperado
            LLVMTypeRef baseType = LLVM.TypeOf(baseValue);
            LLVMTypeRef exponentType = LLVM.TypeOf(exponentValue);

            if (baseType != exponentType)
            {
                throw new Exception("Error: El tipo de la base y el exponente deben ser iguales.");
            }

            // Crear los argumentos para la llamada
            LLVMValueRef[] args = new LLVMValueRef[] { baseValue, exponentValue };

            // Usar un bloque unsafe para trabajar con punteros
            unsafe
            {
                // Obtener un puntero a los argumentos (arreglo de punteros)
                fixed (LLVMValueRef* argsPtr = args)
                {
                    // Si ambos valores son de tipo float
                    if (baseType.Equals(LLVMTypeRef.Float))
                    {
                        // Llamar a llvm.pow.f32
                        return LLVM.BuildCall2(
                            builder, // builder
                            LLVMTypeRef.CreateFunction(LLVMTypeRef.Float, new LLVMTypeRef[] { LLVMTypeRef.Float, LLVMTypeRef.Float }, true), // tipo de la funcion
                            LLVM.GetNamedFunction(module, SByteConvert("llvm.pow.f32")), // función llvm.pow.f32
                            (LLVMOpaqueValue**)argsPtr, // puntero a los argumentos (ahora correctamente pasado)
                            (uint)args.Length, // cantidad de argumentos
                            SByteConvert("pow_tmp") // nombre de la instrucción
                        );
                    }
                    // Si ambos valores son de tipo double
                    else if (baseType.Equals(LLVMTypeRef.Double))
                    {
                        // Llamar a llvm.pow.f64
                        return LLVM.BuildCall2(
                            builder, // builder
                            LLVMTypeRef.CreateFunction(LLVMTypeRef.Double, new LLVMTypeRef[] { LLVMTypeRef.Double, LLVMTypeRef.Double }, true), // tipo de la funcion
                            LLVM.GetNamedFunction(module, SByteConvert("llvm.pow.f64")), // función llvm.pow.f64
                            (LLVMOpaqueValue**)argsPtr, // puntero a los argumentos (ahora correctamente pasado)
                            (uint)args.Length, // cantidad de argumentos
                            SByteConvert("pow_tmp") // nombre de la instrucción
                        );
                    }
                    else
                    {
                        throw new Exception("Error: El tipo de base o exponente no soportado para exponenciación.");
                    }
                }
            }
        }


        // Función auxiliar para obtener el tipo LLVM según VariableType
        static LLVMTypeRef GetLLVMType(VariableType type)
        {
            return type switch
            {
                VariableType.Int => LLVMTypeRef.Int32,
                VariableType.Float => LLVMTypeRef.Float,
                VariableType.Char => LLVMTypeRef.Int8,
                VariableType.String => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                VariableType.Bool => LLVMTypeRef.Int1,
                _ => throw new Exception("Tipo no soportado"),
            };
        }

        public static void GenerateReturn(string value)
        {
            value = value.Trim(); // Eliminar espacios innecesarios

            // Si es un número entero
            if (int.TryParse(value, out int intValue))
            {
                LLVMValueRef returnValue = LLVM.ConstInt(LLVMTypeRef.Int32, (ulong)intValue, 1);
                LLVM.BuildRet(builder, returnValue);
                return;
            }

            // Si es un número flotante
            if (float.TryParse(value, out float floatValue))
            {
                LLVMValueRef returnValue = LLVM.ConstReal(LLVMTypeRef.Float, (double)floatValue);
                LLVM.BuildRet(builder, returnValue);
                return;
            }

            // Si es un valor booleano (true/false)
            if (value.ToLower() == "true" || value.ToLower() == "false")
            {
                LLVMValueRef returnValue = LLVM.ConstInt(LLVMTypeRef.Int1, value.ToLower() == "true" ? 1UL : 0UL, 0);
                LLVM.BuildRet(builder, returnValue);
                return;
            }

            // Si es una cadena (string) entre comillas
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                // Eliminar las comillas
                string stringValue = value.Trim('"');

                // Crear una cadena global en LLVM
                LLVMValueRef returnValue = LLVM.BuildGlobalStringPtr(builder, SByteConvert(stringValue), SByteConvert("string_tmp"));
                LLVM.BuildRet(builder, returnValue);
                return;
            }

            // Si es una variable, cargar su valor
            if (variables.TryGetValue(value, out LLVMValueRef variableRef))
            {
                LLVMValueRef loadedVar = LLVM.BuildLoad2(builder, LLVMTypeRef.Int32, variableRef, SByteConvert(value));
                LLVM.BuildRet(builder, loadedVar);
                return;
            }

            throw new Exception($"Error: Token no reconocido en expresión: '{value}'.");
        }


        #region Generate
        public static void Generate()
        {
            Console.WriteLine();

            // Guardar el archivo output.ll
            module.PrintToFile(outputLlvmFile);

            if (File.Exists(outputLlvmFile))
            {
                Console.WriteLine($"Archivo LLVM IR generado: {outputLlvmFile}");

                // Compilar el LLVM IR a ejecutable usando clang
                bool statusCompile = RunProcess("C:\\Program Files\\LLVM\\bin\\clang", $"{outputLlvmFile} -o {outputExeFile}");

                if (statusCompile)
                {
                    Console.WriteLine();
                    Console.WriteLine("=============================================");
                    Console.WriteLine("===========  EJECUTANDO EXE  ================");
                    Console.WriteLine("=============================================");
                    Console.WriteLine();
                    // Ejecutar el ejecutable generado
                    RunProcess($"./{outputExeFile}", "");
                }
            }
        }

        static bool RunProcess(string command, string args)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = args;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true; // Capturar errores
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine(output);

                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($"Error: {error}");

                if(!string.IsNullOrEmpty(error))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ejecutando {command}: {ex.Message}");
                return false;
            }
        }


        #endregion
    }
}
