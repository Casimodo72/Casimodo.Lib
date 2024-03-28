using Casimodo.Lib.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.SimpleParser
{
    public class SimpleParserException : ParserException
    {
        public SimpleParserException() { }
        public SimpleParserException(string message) : base(message) { }
        public SimpleParserException(string message, Exception inner) : base(message, inner) { }
    }

    public class SimpleParserBase
    {
        public Func<string[], string> LookupVariableValue;

        protected string[] Tokens;

        protected string[] Tokenize(string input, string[] delimiters)
        {
            // Yes, we could use RegEx, but this one is faster for small data sets.
            // http://stackoverflow.com/questions/2484919/how-do-i-split-a-string-by-strings-and-include-the-delimiters-using-net

            int[] nextPosition = delimiters.Select(d => input.IndexOf(d)).ToArray();
            List<string> result = new List<string>(16);
            int pos = 0;
            while (true)
            {
                int firstPos = int.MaxValue;
                string delimiter = null;
                for (int i = 0; i < nextPosition.Length; i++)
                {
                    if (nextPosition[i] != -1 && nextPosition[i] < firstPos)
                    {
                        firstPos = nextPosition[i];
                        delimiter = delimiters[i];
                    }
                }

                if (firstPos != int.MaxValue)
                {
                    result.Add(input.Substring(pos, firstPos - pos));
                    result.Add(delimiter);
                    pos = firstPos + delimiter.Length;

                    for (int i = 0; i < nextPosition.Length; i++)
                    {
                        if (nextPosition[i] != -1 && nextPosition[i] < pos)
                        {
                            nextPosition[i] = input.IndexOf(delimiters[i], pos);
                        }
                    }
                }
                else
                {
                    result.Add(input.Substring(pos));
                    break;
                }
            }

            return [.. result];
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected void ThrowUnexpectedCurToken()
        {
            throw new ParserException(
                string.Format("Unexpected token '{0}'", CurToken));
        }

        protected void ThrowUnexpectedCurToken(string expectedToken)
        {
            throw new ParserException(
                string.Format(
                    "Unexpected token '{0}'. Expected was token '{1}'.",
                    CurToken, expectedToken));
        }

        protected int ConvertPropertyIdToInteger(string value)
        {
            if (!int.TryParse(value, out int result))
                throw new ParserException(
                    string.Format(
                        "The specified property ID '{0}' is not a valid integer value.",
                        value));

            return result;
        }

        protected void NextToken(string expectedCurrentToken)
        {
            CheckToken(expectedCurrentToken);
            NextToken();
        }

        protected void CheckToken(string expectedCurrentToken)
        {
            CheckNotEoT();
            if (CurToken != expectedCurrentToken)
                ThrowUnexpectedCurToken(expectedCurrentToken);
        }

        protected void CheckNotToken(string notExpectedCurrentToken)
        {
            CheckNotEoT();
            if (CurToken == notExpectedCurrentToken)
                ThrowUnexpectedCurToken();
        }

        protected void CheckNotEoT()
        {
            if (IsEoT)
                throw new ParserException(
                    string.Format("Unexpected end of expression."));
        }

        protected int CurPos;

        protected bool IsEoT
        {
            get { return CurPos >= Tokens.Length; }
        }

        protected void NextToken()
        {
            CurPos++;
        }

        protected string CurToken
        {
            get { return Tokens[CurPos]; }
        }

        protected string PeekToken(int offset)
        {
            int index = CurPos + offset;
            if (index >= Tokens.Length)
                return null;
            return Tokens[index];
        }
    }
}