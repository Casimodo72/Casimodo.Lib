using Casimodo.Lib.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.SimpleParser
{
    public class SimpleStringParser
    {
        public string Text;
        public int CurPos;
        public readonly StringBuilder TextBuilder;

        public SimpleStringParser()
        {
            TextBuilder = new StringBuilder();
        }

        public SimpleStringParser(string text)
        {
            TextBuilder = new StringBuilder(text.Length);
            Initialize(text);
        }

        public void Initialize(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            CurPos = 0;
            TextBuilder.Length = 0;
            Text = text;
        }

        /// <summary>
        /// Returns the currently consumed text and clears the consumed text.
        /// </summary>
        /// <returns></returns>
        public string GetConsumedText(bool clear = true)
        {
            var result = TextBuilder.ToString();
            if (clear)
                TextBuilder.Length = 0;

            return result;
        }

        public void ClearConsumedText()
        {
            TextBuilder.Length = 0;
        }

        public void CheckNotEnd()
        {
            if (IsEnd) throw new ParserException("Unexpected end of expression.");
        }

        public bool IsEnd
        {
            get { return CurPos >= Text.Length; }
        }

        public bool IsNot(params char[] chars)
        {
            if (IsEnd) return false;
            if (chars == null || chars.Length == 0)
                return true;

            return !chars.Any(x => x == Cur);
        }

        public bool Is(params char[] chars)
        {
            if (IsEnd) return false;
            if (chars == null || chars.Length == 0)
                return false;

            return chars.Any(x => x == Cur);
        }

        public bool Is(string text)
        {
            return !IsEnd && string.CompareOrdinal(Text, CurPos, text, 0, text.Length) == 0;
        }

        public void SkipWsp()
        {
            while (!IsEnd && char.IsWhiteSpace(Cur))
                Next();
        }

        public void Next(int offset = 1)
        {
            CurPos += offset;
            RestrictCurPos();
        }

        void RestrictCurPos()
        {
            if (CurPos > Text.Length)
                CurPos = Text.Length;
        }

        public bool Skip(char ch, bool caseSensitive = true)
        {
            if (!CheckExpected(ch, caseSensitive))
                return false;

            Next();
            return true;
        }

        bool CheckExpected(char ch, bool caseSensitive = true)
        {
            return !IsEnd && ((caseSensitive && ch == Cur) || (!caseSensitive && char.ToLower(ch) == char.ToLower(Cur)));
        }

        public bool Skip(string text, bool caseSensitive = true)
        {
            if (!CheckExpected(text, caseSensitive))
                return false;

            Next(text.Length);
            return true;
        }
        bool CheckExpected(string text, bool caseSensitive = true)
        {
            if (IsEnd)
                return false;

            var current = PeekNext(text.Length);
            if (current == null)
                return false;

            return !IsEnd && ((caseSensitive && text == current) || (!caseSensitive && current.ToLower() == text.ToLower()));
        }


        public bool IsDigit
        {
            get { return !IsEnd && char.IsDigit(Cur); }
        }

        public bool IsLetter
        {
            get { return !IsEnd && char.IsLetter(Cur); }
        }

        char Cur
        {
            get { return Text[CurPos]; }
        }

        public char CurNext()
        {
            var cur = Cur;
            Next();
            return cur;
        }

        public char? Peek(int offset)
        {
            int index = CurPos + offset;
            if (index >= Text.Length)
                return null;

            return Text[index];
        }

        public string PeekNext(int length)
        {
            if (CurPos + length - 1 >= Text.Length)
                return null;

            return Text.Substring(CurPos, length);
        }

        public bool MoveBackwardsTo(int pos)
        {
            if (pos >= CurPos || pos < 0 || pos >= Text.Length)
                return false;

            CurPos = pos;

            return true;
        }

        /// <summary>
        /// Moves forwards only.
        /// </summary>
        public bool MoveTo(int pos)
        {
            if (IsEnd) return false;

            if (pos < CurPos || pos < 0 || pos >= Text.Length)
                return false;

            CurPos = pos;

            return true;
        }

        public bool MoveTo(string text)
        {
            if (IsEnd) return false;

            var pos = Text.IndexOf(text, CurPos);
            if (pos < 0)
                return false;

            return MoveTo(pos);
        }

        /// <summary>
        /// Consumes all subsequent characters before the specified position (zero based).
        /// </summary>
        public bool ConsumeTo(int pos)
        {
            if (IsEnd) return false;

            var start = CurPos;
            if (!MoveTo(pos))
                return false;

            TextBuilder.Append(Text, start, CurPos - start);

            return true;
        }

        /// <summary>
        /// Consumes all subsequent charaters before the specified character.
        /// </summary>
        public bool ConsumeTo(string text)
        {
            if (IsEnd) return false;

            var pos = Text.IndexOf(text, CurPos);
            if (pos < 0)
                return false;

            return ConsumeTo(pos);
        }

        public bool Skip(int length = 1)
        {
            return MoveTo(CurPos + length);
        }

        public bool Consume(int length = 1)
        {
            if (length == 1)
            {
                if (CurPos >= Text.Length) return false;
                TextBuilder.Append(Text[CurPos]);
                Next();
                return true;
            }
            else
                return ConsumeTo(CurPos + length);
        }

        public bool ConsumeWsp()
        {
            bool consumed = false;
            while (!IsEnd && char.IsWhiteSpace(Cur))
            {
                Consume();
                consumed = true;
            }

            return consumed;
        }

        public bool Consume(string text)
        {
            if (!Is(text)) return false;

            return Consume(text.Length);
        }

        public bool Consume(char ch, bool caseSensitive = true)
        {
            if (!CheckExpected(ch, caseSensitive))
                return false;

            TextBuilder.Append(Cur);
            Next();
            return true;
        }

        public bool ReadQuoted(out string text)
        {
            text = null;
            if (!IsAnyQuotationMark()) return false;

            Next();

            var sb = new StringBuilder();

            while (!IsEnd && !IsAnyQuotationMark())
            {
                sb.Append(Cur);
                Next();
            }

            if (!IsAnyQuotationMark())
                return false;

            Next();

            text = sb.ToString();
            return true;
        }

        public bool IsAnyQuotationMark()
        {
            return !IsEnd && Cur == '\"' || Cur == '\'';
        }

        public bool ConsumeAnyQuotationMark()
        {
            if (!IsAnyQuotationMark())
                return false;

            Consume();
            return true;
        }

        public bool ConsumeToEnd()
        {
            if (IsEnd) return false;

            TextBuilder.Append(Text, CurPos, Text.Length - CurPos);
            CurPos = Text.Length;

            return true;
        }

        public void o(string text)
        {
            TextBuilder.Append(text);
        }
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

            return result.ToArray();
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
            int result;
            if (!int.TryParse(value, out result))
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