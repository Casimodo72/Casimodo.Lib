using Casimodo.Lib.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#nullable enable

namespace Casimodo.Lib.Templates
{
    public enum TemplateNodeKind
    {
        None,
        Text,
        ValueTemplate,
        Expression,
        CSharpExpression
    }

    public class TemplateNode
    {
        public TemplateNodeKind Kind { get; set; } = TemplateNodeKind.None;
        public string Expression { get; set; } = "";
    }

    public class TemplateExpression : TemplateNode
    {
        public TemplateExpression()
        {
            Kind = TemplateNodeKind.Expression;
        }
    }

    public class TemplateValueTemplate : TemplateExpression
    {
        public TemplateValueTemplate()
        {
            Kind = TemplateNodeKind.ValueTemplate;
        }
    }

    public class TemplateElement : TemplateExpression
    {
        public bool IsForeach { get; set; }
        public bool IsCondition { get; set; }
        public string? ValueTemplateName { get; set; }
    }

    public class TemplateEnvContainer
    {
        public TemplateExpressionContext Context
        {
            get
            {
                return _context
                    ?? throw new TemplateException("Context not assigned on environment container.");
            }
            set => _context = value;
        }
        TemplateExpressionContext? _context;
    }

    public sealed class TemplateExternalPropertiesContainer
    {
        public List<TemplateExternalProperty> Items { get; set; } = [];
    }

    /// <summary>
    /// Additional global properties (and values) provided by external means.
    /// E.g. custom properties defined by the FlexEmailDocumentTemplate itself.
    /// </summary>
    public sealed class TemplateExternalProperty(string name, string displayName, string kind, string type,
        bool isInput, string value)
    {
        public Guid Guid { get; } = Guid.NewGuid();
        public string Name { get; } = name;
        public string DisplayName { get; } = displayName;
        public string Kind { get; } = kind;
        public string Type { get; } = type;
        public bool IsInput { get; } = isInput;
        public string Value { get; set; } = value;
    }

    public class TemplateExpressionContext
    {
        public TemplateExpressionContext(TemplateContext coreContext, TemplateDataContainer data,
            TemplateProcessor processor, AstNode ast)
        {
            CoreContext = coreContext;
            DataContainer = data;
            Processor = processor;
            Ast = ast;
        }

        public bool IsMatch { get; set; }

        public bool IsModificationDenied { get; set; }

        public bool HasReturnValue { get; private set; }

        public void SetReturnValue(IEnumerable<object?> value)
        {
            ReturnValue = value ?? Enumerable.Empty<object?>();
            HasReturnValue = true;
        }

        public IEnumerable<object?> ReturnValue { get; private set; } = Enumerable.Empty<object?>();

        public AstNode Ast { get; }

        public InstructionAstNode? CurrentInstruction { get; set; }

        List<object> Services { get; set; } = new List<object>(3);

        public TemplateContext CoreContext { get; }

        /// <summary>
        /// NOTE: Will be null when evaluating an expression is not expected
        /// to modify the output.
        /// </summary>
        public TemplateProcessor Processor { get; }

        public TemplateDataContainer DataContainer { get; }

        public void Use(object service)
        {
            if (service == null)
                return;

            Services.Add(service);
        }

        public T Get<T>() where T : class
        {
            var type = typeof(T);
            var item = Services.FirstOrDefault(x => x.GetType().IsAssignableFrom(type));
            if (item == null)
                throw new TemplateException($"An instance of type '{type.FullName}' was not found.");

            return (T)item;
        }
    }

    public class AstNode
    {
        public AstNode(AstTypeInfo returnType)
        {
            ReturnType = returnType;
        }

        public AstNode? Left { get; set; }
        public AstNode? Right { get; set; }

        public bool IsOp { get; set; }

        public bool IsLiteral { get; set; }

        public string? TextValue { get; set; }

        public AstTypeInfo ReturnType { get; }
    }

    public class CSharpScriptAstNode : AstNode
    {
        public CSharpScriptAstNode(CSharpScriptWrapper script)
            : base(AstTypeInfo.None)
        {
            Script = script;
        }

        public CSharpScriptWrapper Script { get; }
    }

    public class AstTypeInfo
    {
        class NoTypeType
        { }

        internal static Type NoType = typeof(NoTypeType);

        public AstTypeInfo(Type type)
        {
            Type = type;
        }

        internal static readonly AstTypeInfo None = new(NoType);
        internal static readonly AstTypeInfo String = new(typeof(string)) { IsSimpleType = true };
        public bool IsListType { get; set; }
        public bool IsSimpleType { get; set; }
        public Type Type { get; }
    }

    // TODO: REMOVE
    //public class FunctionAstNode : AstNode
    //{
    //    public Type SourceType { get; set; }
    //    public string Name { get; set; }
    //    public TemplateInstructionDefinition Definition { get; set; }
    //}

    public abstract class InstructionAstNode : AstNode
    {
        public InstructionAstNode(Type sourceType, string name, AstTypeInfo returnType)
            : base(returnType)
        {
            SourceType = sourceType;
            Name = name;
        }

        public Type SourceType { get; }
        public string Name { get; }
    }

    public sealed class PropertyAstNode : InstructionAstNode
    {
        public PropertyAstNode(Type sourceType, PropertyInfo propInfo, AstTypeInfo returnType)
            : base(sourceType, propInfo.Name, returnType)
        {
            PropInfo = propInfo;
        }

        public PropertyInfo PropInfo { get; }
    }

    public sealed class InstructionDefinitionAstNode : InstructionAstNode
    {
        public InstructionDefinitionAstNode(
            Type sourceType,
            TemplateInstructionDefinition definition, AstTypeInfo returnType)
            : base(sourceType, definition.Name, returnType)
        {
            Definition = definition;
        }

        public TemplateInstructionDefinition Definition { get; }
    }

    public sealed class FormatValueAstNode : AstNode
    {
        public FormatValueAstNode(string format, AstTypeInfo returnType)
            : base(returnType)
        {
            Format = format;
        }

        public string Format { get; }
    }

    public sealed class TemplateInstructionDefinition<TSource> : TemplateInstructionDefinition
    {
        public TemplateInstructionDefinition(Type returnType)
            : base(typeof(TSource), returnType)
        { }

        public Func<TemplateExpressionContext, TSource, object?>? ValueGetter { get; set; }
        public Func<TemplateExpressionContext, TSource, IEnumerable<object>>? ListValueGetter { get; set; }

        public override IEnumerable<object?> GetValuesCore(TemplateExpressionContext context, object item)
        {
            if (ValueGetter != null)
            {
                var result = ValueGetter(context, (TSource)item);

                if (result is not string && result is IEnumerable enumerable)
                    return enumerable.Cast<object>();
                else
                    return Enumerable.Repeat(result, 1);
            }
            else if (ListValueGetter != null)
            {
                return ListValueGetter(context, (TSource)item);
            }
            else throw new TemplateException(
                "The template instruction has neither a value nor a list-value getter assigned.");
        }
    }

    /// <summary>
    /// Currently only global void functions are needed.
    /// </summary>
    public class TemplateFunctionDefinition
    {
        public TemplateFunctionDefinition(string name)
        {
            Guard.ArgNotEmpty(name, nameof(name));

            Name = name;
        }

        public string Name { get; }

        public Action<TemplateExpressionContext, object>? ExecuteCore { get; set; }
    }

    public abstract class TemplateInstructionDefinition
    {
        public TemplateInstructionDefinition(Type sourceType, Type returnType)
        {
            SourceType = sourceType;
            ReturnType = returnType;
        }

        public Type SourceType { get; }
        public required string Name { get; set; }
        public Type ReturnType { get; set; }
        public bool IsReturnTypeList { get; set; }
        public bool IsReturnTypeSimple { get; set; }

        public Action<TemplateExpressionContext, object>? ExecuteCore { get; set; }

        public abstract IEnumerable<object?> GetValuesCore(TemplateExpressionContext context, object item);
    }
}
