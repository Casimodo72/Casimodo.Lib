using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Casimodo.Lib.Templates
{
    public class TemplateElemTransformationContext
    {
        public TemplateTransformation Transformation { get; set; }
        public bool IsMatch { get; set; }
        public List<object> Values { get; set; } = new List<object>();
        public SelectorAstItem Ast { get; set; }
    }

    public class SelectorAstItem
    {
        public SelectorAstItem Left { get; set; }
        public SelectorAstItem Right { get; set; }
    }

    public class FuncAstItem : SelectorAstItem
    {
        public string Name { get; set; }
        public PropStepFuncDef Definition { get; set; }

        public void Execute(TemplateElemTransformationContext context)
        {
            Definition.Execute(context);
        }
    }

    public class PropAstItem : SelectorAstItem
    {
        public Type SourceType { get; set; }
        //public object SourceValue { get; set; }
        public Type TargetType { get; set; }
        public string SourceProp { get; set; }
        public PropertyInfo SourcePropInfo { get; set; }
        //public object TargetValue { get; set; }
        public bool IsLeafValue { get; set; }
        public bool IsList { get; set; }
        public TemplateStepCustomPropBase CustomDefinition { get; set; }
        public List<PropStepFuncDef> Functions { get; set; }
    }

    public class PropStepFuncDef
    {
        public string Name { get; set; }
        public bool IsListFunc { get; set; }

        public void Execute(TemplateElemTransformationContext context)
        {
            if (Name == "Join")
            {
                var value = context.Values.Select(x => x.ToString()).Join("; ");

                context.Values.Clear();
                context.Values.Add(value);
            }
        }
    }

    public sealed class TemplateStepCustomProp<TTrans, TSource> : TemplateStepCustomPropBase
        where TTrans : TemplateTransformation
    {
        public TemplateStepCustomProp()
        {
            SourceType = typeof(TSource);
        }

        public Func<TSource, object> GetTargetValue { get; set; }
        public Func<TSource, IEnumerable<object>> GetTargetValues { get; set; }

        public Action<TTrans, TSource> Execute { get; set; }

        public override IEnumerable<object> GetTargetValuesCore(object item)
        {
            if (GetTargetValue != null)
                return Enumerable.Repeat(GetTargetValue((TSource)item), 1);

            return GetTargetValues((TSource)item);
        }
    }

    //public sealed class TemplateStepCustomProp : TemplateStepCustomPropBase
    //{
    //    public Func<object, object> GetTargetValue { get; set; }
    //    public Func<object, IEnumerable<object>> GetTargetValues { get; set; }

    //    public override IEnumerable<object> GetTargetValuesCore(object item)
    //    {
    //        if (GetTargetValue != null)
    //            return Enumerable.Repeat(GetTargetValue(item), 1);

    //        return GetTargetValues(item);
    //    }
    //}

    public abstract class TemplateStepCustomPropBase
    {
        public Type SourceType { get; set; }
        public string PropName { get; set; }
        public bool IsList { get; set; }
        public Type TargetType { get; set; }
        public bool IsLeafValue { get; set; }

        public Action<TemplateTransformation, object> ExecuteCore { get; set; }

        public abstract IEnumerable<object> GetTargetValuesCore(object item);
    }
}
