using Casimodo.Lib.CSharp;
using Casimodo.Lib.SimpleParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Casimodo.Lib.Templates
{
    public class TemplateException : Exception
    {
        public TemplateException() { }
        public TemplateException(string message) : base(message) { }
    }

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

    public class TemplateExpressionFactory
    {
        const string CSharpExpressionPrefix = "cs:";

        public static T CreateExpression<T>(string expression, TemplateNodeKind? kind = null, bool isAttrOrigin = false)
            where T : TemplateExpression, new()
        {
            var item = new T();

            item.Kind = kind ?? TemplateNodeKind.Expression;

            expression = expression?.Trim();

            if (expression?.StartsWith(CSharpExpressionPrefix) == true)
            {
                if (item.Kind != TemplateNodeKind.Expression)
                    throw new TemplateProcessorException(
                        $"Template node of kind '{item.Kind}' must not start with the " +
                        $"CSharp expression prefix '{CSharpExpressionPrefix}'.");

                item.Kind = TemplateNodeKind.CSharpExpression;

                expression = expression.Substring(CSharpExpressionPrefix.Length);

                if (isAttrOrigin)
                {
                    // KABU TODO: IMPORTANT: Maybe we should force putting C# expression into element content
                    //   rather than having it on and XML attribute where we need to convert to double quotes,
                    //   plus can't use single quotes.
                    expression = expression.Replace("'", "\"");
                }
            }
            else
            {
                item.Kind = TemplateNodeKind.Expression;
            }

            item.Expression = expression;

            return item;
        }
    }

    // KABU TODO: Do we want to implement functions like "EnableArea(Contract.InvoiceRecipient)" ?.
    public class TemplateExpressionParser : SimpleStringTokenParser
    {
        public CSharpScriptOptionsWrapper CSharpScriptOptions { get; set; }

        public List<ITemplateInstructionResolver> InstructionResolvers { get; set; } = new List<ITemplateInstructionResolver>();

        AstTypeInfo CurType { get; set; }

        void Tokenize(string expression)
        {
            // NOTE: The following chars must be escaped: \ * + ? | { [ ( ) ^ $ . # " and space.
            Regex regex = new Regex(@"([\.])");

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

        public AstNode ParseTemplateExpression(TemplateDataContainer data, string expression, TemplateNodeKind kind)
        {
            if (kind == TemplateNodeKind.CSharpExpression)
                return ParseCSharpExpression(data, expression);
            else
                return ParseExpression(data.GetType(), expression);
        }

        public AstNode ParseCSharpExpression(TemplateDataContainer data, string expression)
        {
            var scriptService = new CSharpLanguageServiceWrapper();

            var code = BuildCSharpScript(data, expression);

            var script = new CSharpLanguageServiceWrapper().CompileScript(
                code,
                CSharpScriptOptions,
                typeof(TemplateDataContainer));

            var scriptNode = new CSharpScriptAstNode();
            scriptNode.Script = script;

            return scriptNode;
        }

        public AstNode ParseExpression(Type contextItemType, string expression)
        {
            Guard.ArgNotNull(contextItemType, nameof(contextItemType));

            Clear();
            try
            {
                Tokenize(expression);
                var itype = new AstTypeInfo
                {
                    Type = contextItemType,
                    IsListType = false,
                    IsSimpleType = false
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

        string BuildCSharpScript(TemplateDataContainer data, string expression)
        {
            var sb = new StringBuilder();
            var className = "T" + Guid.NewGuid().ToString().Replace("-", "");
            sb.Append(@"public class " + className + @" { ");
            sb.Append(Environment.NewLine);
            sb.Append("TemplateDataContainer _data;");
            sb.Append(Environment.NewLine);
            sb.Append("public ");
            sb.Append(className);
            sb.Append(@"(TemplateDataContainer data) { _data = data; }");
            sb.Append(Environment.NewLine);

            foreach (var prop in data.GetPropAccessors())
            {
                sb.Append(string.Format(
                    "public {0} {1} {{ get {{ return _data.Prop<{0}>(\"{1}\"); }} }}",
                    prop.Type.FullName, prop.Name));

                sb.Append(Environment.NewLine);
            }

            sb.Append(@"public object Execute() { return ");
            sb.Append(expression);
            sb.Append("; } }");
            sb.Append(Environment.NewLine);
            sb.Append("return new " + className + "(Self).Execute();");

            return sb.ToString();
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
                    // Handle custom instructions (or overrides of declared properties) first.
                    node = ParseCustomInstruction(token);
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

        InstructionAstNode CreatePropNode()
        {
            return new InstructionAstNode();
        }

        InstructionAstNode ParseCustomInstruction(string propName)
        {
            if (InstructionResolvers == null)
                return null;

            TemplateInstructionDefinition definition = null;

            foreach (var resolver in InstructionResolvers)
            {
                definition = resolver.ResolveInstruction(CurType.Type, propName);
                if (definition != null)
                    break;
            }

            if (definition == null)
                return null;

            var node = CreatePropNode();
            node.SourceType = CurType.Type;
            node.Name = definition.Name;
            node.Definition = definition;

            node.ReturnType = new AstTypeInfo
            {
                Type = definition.ReturnType,
                IsListType = definition.IsReturnTypeList,
                IsSimpleType = definition.IsReturnTypeSimple
            };

            return node;
        }

        InstructionAstNode ParseProp(string name)
        {
            var iprop = CurType.Type.GetProperty(name);
            if (iprop == null)
            {

                // Search in interfaces of interface.
                if (CurType.Type.IsInterface)
                {
                    foreach (var iface in CurType.Type.GetInterfaces())
                    {
                        iprop = iface.GetProperty(name);
                        if (iprop != null)
                            break;
                    }
                }

                if (iprop == null)
                    return null;
            }

            var prop = CreatePropNode();
            prop.SourceType = CurType.Type;
            prop.Name = iprop.Name;
            prop.PropInfo = iprop;
            prop.ReturnType = new AstTypeInfo
            {
                Type = iprop.PropertyType,
                IsListType = false,
                IsSimpleType = IsSimple(iprop.PropertyType)
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
