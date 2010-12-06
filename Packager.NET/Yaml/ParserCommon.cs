using System;
using System.Collections.Generic;
using System.Text;
using QiHe.CodeLib;

namespace Yaml.Grammar
{
    public partial class YamlParser
    {
        int position;

        public int Position
        {
            get { return position; }
            set { position = value; }
        }

        ParserInput<Char> Input;

        public List<Pair<int, string>> Errors = new List<Pair<int, string>>();

        public YamlParser() { }

        private void SetInput(ParserInput<Char> input)
        {
            Input = input;
            position = 0;
        }

        private bool TerminalMatch(char ch)
        {
            if (Input.HasInput(position))
            {
                Char symbol = Input.GetInputSymbol(position);
                return ch == symbol;
            }
            return false;
        }

        private bool TerminalMatch(char ch, int pos)
        {
            if (Input.HasInput(pos))
            {
                Char symbol = Input.GetInputSymbol(pos);
                return ch == symbol;
            }
            return false;
        }

        private char MatchTerminal(char ch, out bool success)
        {
            success = false;
            if (Input.HasInput(position))
            {
                Char symbol = Input.GetInputSymbol(position);
                if (ch == symbol)
                {
                    position++;
                    success = true;
                }
                return symbol;
            }
            return default(Char);
        }

        private char MatchTerminalRange(char start, char end, out bool success)
        {
            success = false;
            if (Input.HasInput(position))
            {
                Char symbol = Input.GetInputSymbol(position);
                if (start <= symbol && symbol <= end)
                {
                    position++;
                    success = true;
                }
                return symbol;
            }
            return default(Char);
        }

        private char MatchTerminalSet(string terminalSet, bool isComplement, out bool success)
        {
            success = false;
            if (Input.HasInput(position))
            {
                Char symbol = Input.GetInputSymbol(position);
                bool match = isComplement ? terminalSet.IndexOf(symbol) == -1 : terminalSet.IndexOf(symbol) > -1;
                if (match)
                {
                    position++;
                    success = true;
                }
                return symbol;
            }
            return default(Char);
        }

        private string MatchTerminalString(string terminalString, out bool success)
        {
            int currrent_position = position;
            foreach (char ch in terminalString)
            {
                MatchTerminal(ch, out success);
                if (!success)
                {
                    position = currrent_position;
                    return null;
                }
            }
            success = true;
            return terminalString;
        }

        private void Error(string message)
        {
            Errors.Add(new Pair<int, string>(position, message));
        }

        private void ClearError()
        {
            Errors.Clear();
        }

        public string GetEorrorMessages()
        {
            StringBuilder text = new StringBuilder();
            foreach (Pair<int, string> msg in Errors)
            {
                text.Append(Input.FormErrorMessage(msg.Left, msg.Right));
                text.AppendLine();
            }
            return text.ToString();
        }
    }
}
