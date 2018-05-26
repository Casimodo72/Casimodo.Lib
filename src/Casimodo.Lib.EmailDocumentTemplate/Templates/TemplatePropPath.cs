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
        public AstItem Ast { get; set; }

        List<object> ContextItems { get; set; } = new List<object>(1);

        public void Use(object context)
        {
            if (context == null)
                return;

            ContextItems.Add(context);
        }

        public T Get<T>() where T : class
        {
            var type = typeof(T);
            var item = ContextItems.FirstOrDefault(x => x.GetType() == type);
            if (item == null)
                throw new TemplateProcessorException($"An instance of type '{type.FullName}' was not found.");

            return (T)item;
        }
    }

    public class AstItem
    {
        public AstItem Left { get; set; }
        public AstItem Right { get; set; }

        public bool IsOp { get; set; }

        public bool IsLiteral { get; set; }

        public string TextValue { get; set; }

        public AstTypeInfo ReturnType { get; set; }
    }


    public class AstTypeInfo
    {
        public bool IsList { get; set; }
        public bool IsSimple { get; set; }
        public Type Type { get; set; }
    }

    public class FuncAstNode : AstItem
    {
        public string Name { get; set; }
        public string ArgsExpression { get; set; }
        public AstTypeInfo Type { get; set; }
        public bool IsListResult { get; set; }
        public FuncAstNodeDef Definition { get; set; }

        public void Execute(TemplateElemTransformationContext context)
        {
            Definition.Execute(context);
        }
    }

    public class PropAstNode : AstItem
    {
        public Type DeclaringType { get; set; }
        public string PropName { get; set; }
        public PropertyInfo PropInfo { get; set; }

        public TemplateStepCustomPropBase CustomDefinition { get; set; }
    }

    public class FuncAstNodeDef
    {
        public string Name { get; set; }
        public bool IsListFunc { get; set; }

        public void Execute(TemplateElemTransformationContext context)
        {
            if (Name == "Select")
            {
                // Expected: Property path
                var value = context.Values.Select(x => x.ToString()).Join("; ");

                context.Values.Clear();
                context.Values.Add(value);
            }

            if (Name == "Join")
            {
                var value = context.Values.Select(x => x.ToString()).Join("; ");

                context.Values.Clear();
                context.Values.Add(value);
            }
        }
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
