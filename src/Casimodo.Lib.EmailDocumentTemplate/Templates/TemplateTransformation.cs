using Casimodo.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Templates
{
    public class TemplateTransformationCustomProps
    {
        public List<TemplateStepCustomPropBase> Items { get; set; } = new List<TemplateStepCustomPropBase>();

        public void AddCustomComplexProp<TSourceType, TTargetType>(string name,
            Func<TemplateElemTransformationContext, TSourceType, TTargetType> value = null,
            Func<TemplateElemTransformationContext, TSourceType, IEnumerable<TTargetType>> values = null)
        {
            var item = new TemplateStepCustomProp<TSourceType>
            {
                PropName = name,             
                TargetType = typeof(TTargetType)
            };
            if (value != null)
            {
                item.IsList = false;
                item.GetTargetValue = (c, x) => value(c, x) as object;
            }

            if (values != null)
            {
                item.IsList = true;
                item.GetTargetValues = (c, x) => values(c, x).Cast<object>();
            }

            Items.Add(item);
        }

        public void AddCustomProp<TSourceType>(string props, bool list = false,
            Func<TemplateElemTransformationContext, TSourceType, object> value = null,
            Func<TemplateElemTransformationContext, TSourceType, IEnumerable<object>> values = null,
            Action<TemplateElemTransformationContext, TSourceType> execute = null)
        {
            foreach (var prop in props.Split(","))
            {
                var item = new TemplateStepCustomProp<TSourceType>
                {
                    PropName = prop.Trim(),
                    IsList = list,
                    TargetType = null,
                    IsSimpleType = true,
                    GetTargetValue = value,
                    GetTargetValues = values,
                    Execute = execute
                };

                if (execute != null)
                    item.ExecuteCore = (c, x) => execute(c, (TSourceType)x);

                Items.Add(item);
            }
        }
    }

    public class TemplateTransformation
    {
        public TemplateTransformation(TemplateProcessor processor)
        {
            Guard.ArgNotNull(processor, nameof(processor));

            Processor = processor;
        }

        public TemplateProcessor Processor { get; private set; }

        protected bool Matches(string name)
        {
            return Processor.Matches(name);
        }

        protected bool ContextMatches(string name)
        {
            return Processor.ContextMatches(name);
        }

        public bool ContextMatches(string name, Type type)
        {
            return Processor.ContextMatches(name, type);
        }

        public bool ContextMatches(Type type)
        {
            return Processor.ContextMatches(type);
        }

        public void SetText(object value)
        {
            Processor.SetText(value);
        }

        public void SetText(string value)
        {
            Processor.SetText(value);
        }

        public void SetTextOrRemove(object value)
        {
            Processor.SetTextOrRemove(value);
        }

        public void SetTextNonEmpty(string text)
        {
            Processor.SetTextNonEmpty(text);
        }

        public bool IsEmpty(string value)
        {
            return Processor.IsEmpty(value);
        }


        public void SetDate(DateTimeOffset? value)
        {
            Processor.SetDate(value);
        }

        public void SetZonedTime(DateTimeOffset? value)
        {
            Processor.SetZonedTime(value);
        }

        public void SetZonedDateTime(DateTimeOffset? value, string format = null)
        {
            Processor.SetZonedDateTime(value, format);
        }

        public bool EnableArea(object value)
        {
            return Processor.EnableArea(value);
        }

        public void EnableArea(bool enabled)
        {
            Processor.EnableArea(enabled);
        }

        public bool EnableValue(object value)
        {
            return Processor.EnableValue(value);
        }
    }
}
