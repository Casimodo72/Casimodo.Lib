using Casimodo.Lib.CSharp;
using Casimodo.Lib.SimpleParser;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Casimodo.Lib.Templates
{
    public static class TemplateNodeFactory
    {
        private const string CSharpExpressionPrefix = "cs:";

        public static T Create<T>(string expression, TemplateNodeKind? kind = null)
            where T : TemplateNode, new()
        {
            var item = new T();

            if (kind != null)
                item.Kind = kind.Value;

            expression = expression?.Trim();

            if (expression?.StartsWith(CSharpExpressionPrefix) == true)
            {
                if (item.Kind != TemplateNodeKind.Expression)
                    throw new TemplateException(
                        $"Template node of kind '{item.Kind}' must not start with the " +
                        $"CSharp expression prefix '{CSharpExpressionPrefix}'.");

                item.Kind = TemplateNodeKind.CSharpExpression;

                expression = expression.Substring(CSharpExpressionPrefix.Length);
            }
            else
            {
                item.Kind = TemplateNodeKind.Expression;
            }

            item.Expression = expression;

            return item;
        }
    }

    // KABU TODO: Do we want to implement functions like "EnableArea(Foo.Bar)" ?
    public class TemplateExpressionParser : SimpleStringTokenParser
    {
        public TemplateExpressionParser(TemplateContext context)
        {
            Context = context;
        }

        public CSharpScriptOptionsWrapper CSharpScriptOptions { get; set; }
        public List<ITemplateInstructionResolver> InstructionResolvers { get; set; } = new List<ITemplateInstructionResolver>();
        public TemplateContext Context { get; }

        AstTypeInfo CurType;

        void Tokenize(string expression)
        {
            _tokens.AddRange(new Regex(@"([.:])").Split(expression).Where(x => !string.IsNullOrEmpty(x)));

            // KABU TODO: REMOVE: No functions and thus no literal arguments expected anymore.
#if (false)
            var tokens = regex.Split(expression);

            // Trim non-literal tokens.
            string token;
            bool literal = false;
            for (int i = 0; i < tokens.Length; i++)
            {
                token = tokens[i];
               
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
#endif
        }

        public AstNode ParseTemplateExpression(TemplateDataContainer data, string expression, TemplateNodeKind kind)
        {
            return kind == TemplateNodeKind.CSharpExpression
                ? ParseCSharpExpression(data, expression)
                : ParseExpression(data.GetType(), expression);
        }

        class CodeCacheItem
        {
            public string Code { get; set; }
            public CSharpScriptWrapper Script { get; set; }
        }

        static readonly ConcurrentBag<CodeCacheItem> _codeCache = new();
        const string ComparableScriptClassName = "#Tc6e8b1d6-9e4f-4ef5-bee7-34882f417efa#";

        public AstNode ParseCSharpExpression(TemplateDataContainer data, string expression)
        {
            // We're using the same class name in order to be able to compare the code
            //  and later replace this name with a unique class name.
            var code = BuildCSharpScriptPhaseOne(ComparableScriptClassName, data, expression);
            var script = _codeCache.Where(x => x.Code == code).Select(x => x.Script).FirstOrDefault();

            if (script == null)
            {
                var effectiveCode = code.Replace(ComparableScriptClassName, "T" + Guid.NewGuid().ToString().Replace("-", ""));

                script = new CSharpLanguageServiceWrapper().CompileScript(
                    effectiveCode,
                    CSharpScriptOptions,
                    typeof(TemplateDataContainer));

                _codeCache.Add(new CodeCacheItem { Code = code, Script = script });
            }

            var scriptNode = new CSharpScriptAstNode
            {
                Script = script
            };

            return scriptNode;
        }

        public AstNode ParseExpression(Type contextItemType, string expression)
        {
            Guard.ArgNotNull(contextItemType);

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
                    throw new TemplateException("Invalid expression.");

                return result;
            }
            finally
            {
                Clear();
            }
        }

        static string BuildCSharpScriptPhaseOne(string className, TemplateDataContainer data, string expression)
        {
            var sb = new StringBuilder();
            sb.o(@"public class " + className + @" { ");
            sb.o(Environment.NewLine);
            sb.o("TemplateDataContainer _data;");
            sb.o(Environment.NewLine);
            sb.o("public ");
            sb.o(className);
            sb.o(@"(TemplateDataContainer data) { _data = data; }");
            sb.o(Environment.NewLine);

            foreach (var prop in data.GetPropAccessors())
            {
                sb.o(string.Format(
                    "public {0} {1} {{ get {{ return _data.GetProp<{0}>(\"{1}\"); }} }}",
                    GetScriptableTypeName(prop.Type), prop.Name));

                sb.o(Environment.NewLine);
            }

            sb.o(@"public object Execute() { return ");
            sb.o(expression);
            sb.o("; } }");
            sb.o(Environment.NewLine);
            sb.o("return new " + className + "(Self).Execute();");

            return sb.ToString();
        }

        static string GetScriptableTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                int backtickIndex = type.FullName.IndexOf('`');
                if (backtickIndex <= 0)
                    throw new InvalidOperationException($"Unexpected full name of generic type ({type.FullName}).");

                return type.FullName.Remove(backtickIndex) +
                    $"<{string.Join(",", type.GetGenericArguments().Select(t => GetScriptableTypeName(t)))}>";
            }
            else
            {
                return type.FullName;
            }
        }

        static List<string> _anySpecialTokens = new()
        {
            ".", "(", ")", "=", "!", "\""
        };

        static bool IsSpecialToken(string token)
        {
            return _anySpecialTokens.Contains(token);
        }

        AstNode ParseAny(AstTypeInfo contextType = null)
        {
            // Either a property or a function is expected.
            AstTypeInfo prevItemType = CurType;
            try
            {
                if (contextType != null)
                    CurType = contextType;

                var token = Current();

                if (IsSpecialToken(token))
                    throw new TemplateException("A property, instruction or function was expected.");

                Next();

                // Handle custom instructions (or overrides of declared properties) first.
                AstNode node = ParseCustomInstruction(token);
                node ??= ParseProp(token);

                if (node == null)
                    throw CreateInvalidExpressionException($"Unexpected token '{token}'.");

                CurType = node.ReturnType;

                if (Is("."))
                {
                    if (!Next())
                        throw CreateExpressionEndedUnexpectedlyException();

                    node.Right = ParseAny();
                }
                else if (Is(":"))
                {


                    node.Right = ParseFormatValue();
                }

                return node;
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

        static InstructionAstNode CreateInstructionNode()
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

            var node = CreateInstructionNode();
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

            var prop = CreateInstructionNode();
            prop.SourceType = CurType.Type;
            prop.Name = iprop.Name;
            prop.PropInfo = iprop;
            prop.ReturnType = new AstTypeInfo
            {
                Type = iprop.PropertyType,
                IsListType = iprop.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(iprop.PropertyType),
                IsSimpleType = TypeHelper.IsSimple(iprop.PropertyType)
            };

            return prop;
        }

        FormatValueAstNode ParseFormatValue()
        {
            if (Current() != ":")
                throw CreateInvalidExpressionException("The token ':' was expected.");

            if (!Next())
                throw CreateInvalidExpressionException("A value format was expected.");

            var format = Current();

            if (CurType.Type == typeof(string))
            {
                var stringFormatter = Context.FindStringFormatter(format);
                if (stringFormatter == null)
                {
                    throw new TemplateException(
                        $"No custom string formatter found for format '{format}'.");
                }
            }
            else if (!typeof(IFormattable).IsAssignableFrom(Nullable.GetUnderlyingType(CurType.Type) ?? CurType.Type))
            {
                throw CreateInvalidExpressionException($"A value of type '{CurType.Type.Name}' " +
                        $"cannot be formatted because it does not implement {nameof(IFormattable)}.");
            }

            if (Next())
                throw CreateInvalidExpressionException("Unexpected tokens after format specifier.");

            return new FormatValueAstNode
            {
                Format = format,
                ReturnType = AstTypeInfo.String
            };
        }

        private TemplateException CreateInvalidExpressionException(string message)
        {
            return new TemplateException($"Invalid expression: {message}");
        }

        private TemplateException CreateExpressionEndedUnexpectedlyException()
            => CreateInvalidExpressionException("Expression ended unexpectedly.");

        // KABU TODO: REMOVE: No functions and thus no literal arguments expected anymore.
        //   Use CSharp expressions instead.
#if (false)
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
#endif
    }
}
