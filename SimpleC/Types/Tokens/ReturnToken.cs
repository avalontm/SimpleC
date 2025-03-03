using System.Diagnostics;

namespace SimpleC.Types.Tokens
{
    class ReturnToken : Token
    {
        public ReturnToken(string content, int line, int column) : base(content, line, column)
        {
            Debug.WriteLine(content);
            if (content != ";")
                throw new ArgumentException("The content is no statement seperator.", "content");
        }
    }
}
