using System;

namespace SimpleC.Lexing
{
    [Flags]
    enum CharType
    {
        Unknown = 0x00,
        Alpha = 0x01,
        Numeric = 0x02,
        LineSpace = 0x04,
        NewLine = 0x08,
        Operator = 0x10,
        OpenBrace = 0x20,
        CloseBrace = 0x40,
        ArgSeperator = 0x80,
        StatementSeperator = 0x100,
        Preprocessor = 0x200, // Representa '#'
        SingleLineComment = 0x400, // Representa "//"
        MultiLineComment = 0x800, // Representa "/*"
        StringDelimiter = 0x1000, // Representa comillas dobles (")
        CharDelimiter = 0x2000, // Representa comillas simples (')
        EscapeChar = 0x4000, // Representa el carácter de escape '\'
        CharPointer = 0x8000, // Representa punteros a char ('char*')
        CharArray = 0x10000, // Representa arreglos de char ('char[]')

        // Valores compuestos
        AlphaNumeric = Alpha | Numeric,
        WhiteSpace = LineSpace | NewLine,
        Brace = OpenBrace | CloseBrace,
        MetaChar = Operator | Brace | ArgSeperator | StatementSeperator | Preprocessor | SingleLineComment | MultiLineComment | StringDelimiter | CharDelimiter | EscapeChar | CharPointer | CharArray,
        All = AlphaNumeric | WhiteSpace | MetaChar,
    }
}
