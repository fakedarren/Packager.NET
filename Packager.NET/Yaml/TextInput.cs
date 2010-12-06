using System;
using System.Collections.Generic;
using System.Text;

namespace Yaml.Grammar
{
    public class TextInput : ParserInput<char>
    {
        string InputText;

        List<int> LineBreaks;

        public TextInput(string text)
        {
            InputText = text;

            LineBreaks = new List<int>();
            LineBreaks.Add(0);
            for (int index = 0; index < InputText.Length; index++)
            {
                if (InputText[index] == '\n')
                {
                    LineBreaks.Add(index);
                }
            }
        }

        #region ParserInput<char> Members

        public int Length
        {
            get { return InputText.Length; }
        }

        public bool HasInput(int pos)
        {
            return pos < InputText.Length;
        }

        public char GetInputSymbol(int pos)
        {
            return InputText[pos];
        }

        public void GetLineColumnNumber(int pos, out int line, out int col)
        {
            col = 1;
            for (line = 1; line < LineBreaks.Count; line++)
            {
                if (LineBreaks[line] > pos)
                {
                    col = pos - LineBreaks[line - 1];
                    break;
                }
            }
        }

        public string GetSubString(int start, int length)
        {
            return InputText.Substring(start, length);
        }

        public string FormErrorMessage(int position, string message)
        {
            int line;
            int col;
            GetLineColumnNumber(position, out line, out col);
            string ch = HasInput(position) ? " '" + GetInputSymbol(position) + "'" : null;
            return String.Format("Line {0}, Col {1}: {2}{3}", line, col, message, ch);
        }

        #endregion
    }
}
