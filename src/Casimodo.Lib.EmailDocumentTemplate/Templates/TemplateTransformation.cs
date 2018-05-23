using Casimodo.Lib;
using System;

namespace Casimodo.Lib.Templates
{
    public class TemplateTransformation
    {
        public TemplateTransformation(TemplateProcessor processor)
        {
            Guard.ArgNotNull(processor, nameof(processor));

            Processor = processor;
        }

        protected TemplateProcessor Processor { get; private set; }

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

        protected void SetText(object value)
        {
            Processor.SetText(value);
        }

        protected void SetText(string value)
        {
            Processor.SetText(value);
        }

        protected void SetTextOrRemove(object value)
        {
            Processor.SetTextOrRemove(value);
        }

        protected void SetTextNonEmpty(string text)
        {
            Processor.SetTextNonEmpty(text);
        }

        protected bool IsEmpty(string value)
        {
            return Processor.IsEmpty(value);
        }

        protected void SetZipCodeCity(string zipcode, string city)
        {
            Processor.SetZipCodeCity(zipcode, city);
        }

        protected void SetDate(DateTimeOffset? value)
        {
            Processor.SetDate(value);
        }

        protected void SetZonedTime(DateTimeOffset? value)
        {
            Processor.SetZonedTime(value);
        }

        protected void SetZonedDateTime(DateTimeOffset? value, string format = null)
        {
            Processor.SetZonedDateTime(value, format);
        }

        protected bool EnableArea(object value)
        {
            return Processor.EnableArea(value);
        }

        protected void EnableArea(bool enabled)
        {
            Processor.EnableArea(enabled);
        }

        protected void EnableValue(bool enabled)
        {
            Processor.EnableValue(enabled);
        }

        protected bool EnableValue(object value)
        {
            return Processor.EnableValue(value);
        }
    }
}
