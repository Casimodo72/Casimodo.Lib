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

    }

    public class TemplateElement : TemplateNode
    {
        public string RootPropertyName { get; set; }
        public object RootContextItem { get; set; }
        public string CurrentPath { get; set; }
    };

    public class TemplateValueTemplate : TemplateNode
    {
        public List<TemplateExpression> Items { get; set; } = new List<TemplateExpression>();
    }

    public class TemplateEnvContainer
    {
        public TemplateElemTransformationContext Context { get; set; }
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
        public string Value { get; set; }
    }



    public class TemplateElemTransformationContext
    {
        public bool IsMatch { get; set; }

        public bool HasValue { get; private set; }

        public void SetValue(object value)
        {
            Value = value;
            HasValue = true;
        }

        public object Value { get; private set; }

        public AstNode Ast { get; set; }

        public InstructionAstNode CurrentInstruction { get; set; }

        List<object> ContextItems { get; set; } = new List<object>(1);

        public TemplateTransformation Transformation { get; set; }

        public TemplateProcessor Processor
        {
            get { return Transformation.Processor; }
        }

        public TemplateDataContainer DataContainer { get; set; }

        public void Use(object context)
        {
            if (context == null)
                return;

            ContextItems.Add(context);
        }

        public T Get<T>() where T : class
        {
            var type = typeof(T);
            var item = ContextItems.FirstOrDefault(x => x.GetType().IsAssignableFrom(type));
            if (item == null)
                throw new TemplateProcessorException($"An instance of type '{type.FullName}' was not found.");

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

        public Func<TemplateElemTransformationContext, TSource, object> GetValue { get; set; }
        public Func<TemplateElemTransformationContext, TSource, IEnumerable<object>> GetValues { get; set; }

        public override IEnumerable<object> GetValuesCore(TemplateElemTransformationContext context, object item)
        {
            if (GetValue != null)
                return Enumerable.Repeat(GetValue(context, (TSource)item), 1);

            return GetValues(context, (TSource)item);
        }
    }

    public abstract class TemplateInstructionDefinition
    {
        public Type SourceType { get; set; }
        public string Name { get; set; }
        public Type ReturnType { get; set; }
        public bool IsListType { get; set; }
        public bool IsSimpleType { get; set; }

        public Action<TemplateElemTransformationContext, object> ExecuteCore { get; set; }

        public abstract IEnumerable<object> GetValuesCore(TemplateElemTransformationContext context, object item);
    }
}
