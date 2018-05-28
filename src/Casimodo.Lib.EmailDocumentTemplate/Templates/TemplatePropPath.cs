using Casimodo.Lib.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Casimodo.Lib.Templates
{
    public class TemplateElemTransformationContext
    {
        public bool IsMatch { get; set; }
        public List<object> Values { get; set; } = new List<object>();
        public AstNode Ast { get; set; }

        List<object> ContextItems { get; set; } = new List<object>(1);

        public TemplateTransformation Transformation { get; set; }

        public TemplateProcessor Processor
        {
            get { return Transformation.Processor; }
        }

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

    public class CSharpScriptAstItem : AstNode
    {
        public CSharpScriptWrapper Script { get; set; }
    }

    public class AstTypeInfo
    {
        public bool IsList { get; set; }
        public bool IsSimple { get; set; }
        public Type Type { get; set; }
    }

    public class PropAstNode : AstNode
    {
        public Type DeclaringType { get; set; }
        public string PropName { get; set; }
        public PropertyInfo PropInfo { get; set; }

        public TemplateStepCustomPropBase CustomDefinition { get; set; }
    }

    public sealed class TemplateStepCustomProp<TSource> : TemplateStepCustomPropBase
    {
        public TemplateStepCustomProp()
        {
            DeclaringType = typeof(TSource);
        }

        public Func<TemplateElemTransformationContext, TSource, object> GetTargetValue { get; set; }
        public Func<TemplateElemTransformationContext, TSource, IEnumerable<object>> GetTargetValues { get; set; }

        public Action<TemplateElemTransformationContext, TSource> Execute { get; set; }

        public override IEnumerable<object> GetTargetValuesCore(TemplateElemTransformationContext context, object item)
        {
            if (GetTargetValue != null)
                return Enumerable.Repeat(GetTargetValue(context, (TSource)item), 1);

            return GetTargetValues(context, (TSource)item);
        }
    }

    public abstract class TemplateStepCustomPropBase
    {
        public Type DeclaringType { get; set; }
        public string PropName { get; set; }
        public bool IsList { get; set; }
        public Type TargetType { get; set; }
        public bool IsSimpleType { get; set; }

        public Action<TemplateElemTransformationContext, object> ExecuteCore { get; set; }

        public abstract IEnumerable<object> GetTargetValuesCore(TemplateElemTransformationContext context, object item);
    }
}
