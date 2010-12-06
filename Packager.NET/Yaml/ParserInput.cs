using System;
using System.Collections.Generic;
using System.Text;

namespace Yaml.Grammar
{
    public interface ParserInput<T>
    {
        int Length { get; }

        bool HasInput(int pos);

        T GetInputSymbol(int pos);

        void GetLineColumnNumber(int pos, out int line, out int col);

        string GetSubString(int start, int length);

        string FormErrorMessage(int position, string message);
    }
}
