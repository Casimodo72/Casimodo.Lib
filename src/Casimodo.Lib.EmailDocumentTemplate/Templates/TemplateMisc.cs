using Casimodo.Lib.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        public string Expression { get; set; }
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
    { }

    public class TemplateEnvContainer
    {
        public TemplateExpressionContext Context { get; set; }
    }

    public class TemplateExternalPropertiesContainer
    {
        public List<TemplateExternalProperty> Items { get; set; } = new List<TemplateExternalProperty>();
    }

    /// <summary>
    /// Additional global properties (and values) provided by external means.
    /// E.g. custom properties defined by the FlexEmailDocumentTemplate itself.
    /// </summary>
    public class TemplateExternalProperty
    {
        public TemplateExternalProperty()
        {
            Guid = Guid.NewGuid();
        }

        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public bool IsInput { get; set; }
    }

    public class TemplateExpressionContext
    {
        public bool IsMatch { get; set; }

        public bool IsModificationDenied { get; set; }

        public bool HasReturnValue { get; private set; }

        public void SetReturnValue(IEnumerable<object> value)
        {
            ReturnValue = value ?? Enumerable.Empty<object>();
            HasReturnValue = true;
        }

        public IEnumerable<object> ReturnValue { get; private set; } = Enumerable.Empty<object>();

        public AstNode Ast { get; set; }

        public InstructionAstNode CurrentInstruction { get; set; }

        List<object> Services { get; set; } = new List<object>(3);

        public TemplateTransformation Transformation { get; set; }

        public TemplateProcessor Processor
        {
            get { return Transformation.Processor; }
        }

        public TemplateDataContainer DataContainer { get; set; }

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
        public AstNode Left { get; set; }
        public AstNode Right { get; set; }

        public bool IsOp { get; set; }

        public bool IsLiteral { get; set; }

        public string TextValue { get; set; }

        public AstTypeInfo ReturnType { get; set; }
    }

    public class CSharpScriptAstNode : AstNode
    {
        public CSharpScriptWrapper Script { get; set; }
    }

    public class AstTypeInfo
    {
        public bool IsListType { get; set; }
        public bool IsSimpleType { get; set; }
        public Type Type { get; set; }
    }

    public class FunctionAstNode : AstNode
    {
        public Type SourceType { get; set; }
        public string Name { get; set; }
        public TemplateInstructionDefinition Definition { get; set; }
    }

    public class InstructionAstNode : AstNode
    {
        public Type SourceType { get; set; }
        public string Name { get; set; }
        public PropertyInfo PropInfo { get; set; }
        public TemplateInstructionDefinition Definition { get; set; }
    }

    public sealed class TemplateInstructionDefinition<TSource> : TemplateInstructionDefinition
    {
        public TemplateInstructionDefinition()
        {
            SourceType = typeof(TSource);
        }

        public Func<TemplateExpressionContext, TSource, object> GetValue { get; set; }
        public Func<TemplateExpressionContext, TSource, IEnumerable<object>> GetValues { get; set; }

        public override IEnumerable<object> GetValuesCore(TemplateExpressionContext context, object item)
        {
            if (GetValue != null)
                return Enumerable.Repeat(GetValue(context, (TSource)item), 1);

            return GetValues(context, (TSource)item);
        }
    }

    /// <summary>
    /// Currently only global void functions are needed.
    /// </summary>
    public class TemplateFunctionDefinition
    {
        public string Name { get; set; }

        public Action<TemplateExpressionContext, object> ExecuteCore { get; set; }
    }

    public abstract class TemplateInstructionDefinition
    {
        public Type SourceType { get; set; }
        public string Name { get; set; }
        public Type ReturnType { get; set; }
        public bool IsReturnTypeList { get; set; }
        public bool IsReturnTypeSimple { get; set; }

        public Action<TemplateExpressionContext, object> ExecuteCore { get; set; }

        public abstract IEnumerable<object> GetValuesCore(TemplateExpressionContext context, object item);
    }
}
