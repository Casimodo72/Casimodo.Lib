using Casimodo.Lib.ComponentModel;
using Casimodo.Lib.CSharp;
using Casimodo.Lib.SimpleParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Casimodo.Lib.Templates
{
    public class SimpleStringTokenParser
    {
        protected readonly List<string> _tokens = new List<string>();
        protected int _curPos;

        public void Initialize(string text)
        {
            Guard.ArgNotNull(text, nameof(text));

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


    public class TemplateExpressionException : Exception
    {
        public TemplateExpressionException() { }
        public TemplateExpressionException(string message) : base(message) { }
    }

    public class TemplatePropPathParser : SimpleStringTokenParser
    {
        AstTypeInfo CurType { get; set; }

        public List<TemplateStepCustomPropBase> CustomPropDefinitions { get; set; }

        void Tokenize(string expression)
        {
            // NOTE: The following chars must be escaped: \ * + ? | { [ ( ) ^ $ . # " and space.
            Regex regex = new Regex(@"([\.)");

            var tokens = regex.Split(expression);

            // Trim non-literal tokens.
            string token;
            bool literal = false;
            for (int i = 0; i < tokens.Length; i++)
            {
                token = tokens[i];

                // KABU TODO: REMOVE: No literals expected anymore.
                if (token == "\"")
                    literal = !literal;
                else
                {
                    if (!literal)
                        token = token.Trim();
                }

                if (string.IsNullOrEmpty(token))
                    continue;

                _tokens.Add(token);
            }
        }

        public List<Type> CompilationReferenceTypes { get; set; } = new List<Type>();

        public CSharpScriptOptionsWrapper ScriptOptions { get; set; }

        public AstNode ParsePropPath(Type contextItemType, TemplateElement element)
        {
            Guard.ArgNotNull(contextItemType, nameof(contextItemType));
            Guard.ArgNotNull(element, nameof(element));

            if (element.IsCSharpExpression)
            {
                var scriptService = new CSharpLanguageServiceWrapper();

                var script = new CSharpLanguageServiceWrapper().CompileScript(
                    element.Expression,
                    ScriptOptions,
                    contextItemType);

                var scriptNode = new CSharpScriptAstItem();
                scriptNode.Script = script;

                return scriptNode;
            }

            Clear();
            try
            {
                Tokenize(element.CurrentPath);
                var itype = new AstTypeInfo
                {
                    Type = contextItemType,
                    IsList = false,
                    IsSimple = false
                };

                var result = ParseAny(itype);

                if (!IsEnd)
                    throw new SimpleParserException("Invalid expression.");

                return result;
            }
            finally
            {
                Clear();
            }
        }

        static List<string> _anySpecialTokens = new List<string>
        {
            ".", "(", ")", "=", "!", "\""
        };

        bool IsSpecialToken(string token)
        {
            return _anySpecialTokens.Contains(token);
        }

        AstNode ParseAny(AstTypeInfo contextType = null)
        {

            // Either property or function is expected.
            AstNode node = null;
            AstTypeInfo prevItemType = CurType;
            try
            {
                if (contextType != null)
                    CurType = contextType;

                var token = Current();

                if (IsSpecialToken(token))
                    throw new SimpleParserException("A property or function was expected.");

                Next();

                if (node == null)
                {
                    // Handle custom properties (or overrides of declared properties) first.
                    node = ParseCustomProp(token);
                    if (node == null)
                        node = ParseProp(token);
                }

                if (node != null)
                {
                    if (Is(".") && Next())
                        node.Right = ParseAny(node.ReturnType);

                    return node;
                }

                throw new SimpleParserException($"Invalid expression '{token}'.");
            }
            finally
            {
                CurType = prevItemType;
            }
        }

        void Clear()
        {
            _tokens.Clear();
            _curPos = 0;
        }

        PropAstNode CreatePropNode()
        {
            return new PropAstNode();
        }

        PropAstNode ParseCustomProp(string expression)
        {
            if (CustomPropDefinitions == null)
                return null;

            var custom = CustomPropDefinitions.FirstOrDefault(x =>
                x.PropName == expression &&
                x.DeclaringType.IsAssignableFrom(CurType.Type));

            if (custom == null)
                return null;

            var node = CreatePropNode();
            node.DeclaringType = CurType.Type;
            node.PropName = custom.PropName;
            node.CustomDefinition = custom;

            node.ReturnType = new AstTypeInfo
            {
                Type = custom.TargetType,
                IsList = custom.IsList,
                IsSimple = custom.IsSimpleType
            };

            return node;
        }

        PropAstNode ParseProp(string name)
        {
            var iprop = CurType.Type.GetProperty(name);
            if (iprop == null)
                return null;

            var prop = CreatePropNode();
            prop.DeclaringType = CurType.Type;
            prop.PropName = iprop.Name;
            prop.PropInfo = iprop;
            prop.ReturnType = new AstTypeInfo
            {
                Type = iprop.PropertyType,
                IsList = false,
                IsSimple = IsSimple(iprop.PropertyType)
            };

            return prop;
        }

        bool IsQuote()
        {
            return Is("\"");
        }

        AstNode TryParseLiteral()
        {
            if (!IsQuote())
                return null;

            Next();

            var items = new List<string>();

            while (!IsQuote())
            {
                items.Add(Current());
                Next();
            }

            var node = new AstNode();
            node.IsLiteral = true;
            node.TextValue = items.Join("");

            Is("\"", required: true);
            Next();

            return node;
        }

        internal static bool IsSimple(Type type)
        {
            // Source: https://stackoverflow.com/questions/863881/how-do-i-tell-if-a-type-is-a-simple-type-i-e-holds-a-single-value

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // Nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }

            return type.IsPrimitive || type.IsEnum || type.Equals(typeof(string)) || type.Equals(typeof(decimal));
        }
    }
}
