using System;
using System.Collections.Generic;

namespace Casimodo.Lib.SimpleParser
{
    public class SimpleStringTokenParser
    {
        protected readonly List<string> _tokens = new List<string>();
        protected int _curPos;

        public void Initialize(IEnumerable<string> tokens)
        {
            Guard.ArgNotNull(tokens, nameof(tokens));

            _tokens.Clear();
            _tokens.AddRange(tokens);
            _curPos = 0;
        }

        public bool IsEnd
        {
            get { return _curPos >= _tokens.Count; }
        }

        public void CheckIs(string text, bool caseSensitive = true)
        {
            if (!Is(text, caseSensitive: caseSensitive))
                ThrowUnexpectedToken(text);
        }

        void ThrowUnexpectedToken(string text)
        {
            throw new SimpleParserException($"Invalid token '{Current()}'. Expected was '{text}'.");
        }

        public bool Skip(string text, bool caseSensitive = true, bool required = false)
        {
            var ok = Is(text, caseSensitive: caseSensitive);

            if (required && !ok)
                ThrowUnexpectedToken(text);

            if (!ok)
                return false;

            Next();

            return true;
        }

        public bool Is(string text, bool caseSensitive = true, bool required = false)
        {
            if (IsEnd) return false;
            if (string.IsNullOrEmpty(text))
                return false;

            bool result;
            if (caseSensitive)
                result = string.Equals(Current(), text, StringComparison.Ordinal);
            else
                result = string.Equals(Current(), text, StringComparison.OrdinalIgnoreCase);

            if (required && !result)
                ThrowUnexpectedEnd();

            return result;
        }

        public string Peek(int offset = 1)
        {
            var pos = _curPos + offset;
            if (pos >= _tokens.Count)
                return null;

            return _tokens[pos];
        }

        void ThrowUnexpectedEnd()
        {
            throw new SimpleParserException("Unexpected end of input.");
        }

        public bool Next(bool required = false)
        {
            if (required && IsEnd)
                ThrowUnexpectedEnd();

            if (_curPos < _tokens.Count)
                _curPos++;

            return !IsEnd;
        }

        public string Current()
        {
            if (IsEnd)
                return null;

            return _tokens[_curPos];
        }
    }
}