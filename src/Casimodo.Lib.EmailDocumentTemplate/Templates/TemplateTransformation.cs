﻿using Casimodo.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Templates
{
    public class TemplateDataContainer
    {
        public TemplateDataContainer Self { get { return this; } }
        readonly List<TemplateDataPropAccessor> _properties = new List<TemplateDataPropAccessor>();

        public void AddProp<T>(string name, T instance = null)
            where T : class, new()
        {
            _properties.Add(new TemplateDataPropAccessor<T>
            {
                Type = typeof(T),
                Name = name,
                InstanceObject = instance
            });
        }

        public void SetProp<T>(object instance)
            where T : class
        {
            var prop = GetPropAccessor(null, type: typeof(T));

            prop.InstanceObject = instance;
        }

        public void SetProp(string name, object instance)
        {
            var prop = GetPropAccessor(name);

            if (instance != null && prop.Type != null && instance.GetType() != prop.Type)
                throw new TemplateProcessorException($"Incorrent property type '{instance.GetType().Name}'. Expected was property of type '{prop.Type}'.");

            prop.InstanceObject = instance;
        }

        public T Prop<T>(string name)
            where T : class
        {
            var prop = GetPropAccessor(name);

            return (T)prop.InstanceObject;
        }
        public T Prop<T>()
            where T : class
        {
            var type = typeof(T);
            var prop = GetPropAccessor(null, type: type);

            return (T)prop.InstanceObject;
        }

        public object Prop(string name, bool defaultIfNull = false)
        {
            var prop = GetPropAccessor(name);

            var value = defaultIfNull
                ? prop.InstanceObjectOrDefault
                : prop.InstanceObject;

            return value;
        }

        public IEnumerable<TemplateDataPropAccessor> GetPropAccessors()
        {
            return _properties;
        }

        public TemplateDataPropAccessor GetPropAccessor(string name, Type type = null, bool required = true)
        {
            if (name == null && type == null)
                throw new ArgumentException("At least one of @name or @type must be specified.");

            var prop = _properties.FirstOrDefault(x =>
                (name == null || x.Name == name) &&
                (type == null || x.Type == type));

            if (prop == null && required)
                throw new TemplateProcessorException($"Data property '{name}' not found.");

            return prop;
        }



    }
    public sealed class TemplateDataPropAccessor<T> : TemplateDataPropAccessor
        where T : class, new()
    {
        public T Instance
        {
            get { return (T)InstanceObject; }
        }

        public T CreateDefault()
        {
            return new T();
        }

        public override object CreateDefaultObject()
        {
            return new T();
        }
    }

    public abstract class TemplateDataPropAccessor
    {
        public TemplateDataPropAccessor()
        {
            Guid = Guid.NewGuid();
        }

        public Guid Guid { get; set; }
        public string Name { get; set; }
        public Type Type { get; set; }

        public object InstanceObjectOrDefault
        {
            get { return InstanceObject ?? CreateDefaultObject(); }
        }

        public object InstanceObject { get; set; }

        public abstract object CreateDefaultObject();
    }

    public interface ICustomInstructionDefinitionResolver
    {
        TemplateInstructionDefinition ResolveProperty(Type sourceType, string propName);
    }

    public class TemplateCustomPropertyDefinitionsContainer : ICustomInstructionDefinitionResolver
    {
        public TemplateInstructionDefinition ResolveProperty(Type sourceType, string propName)
        {
            return Items.FirstOrDefault(x => x.Name == propName && x.SourceType.IsAssignableFrom(sourceType));
        }

        public List<TemplateInstructionDefinition> Items { get; set; } = new List<TemplateInstructionDefinition>();

        public void AddCustomComplexProp<TSourceType, TTargetType>(string name,
            Func<TemplateElemTransformationContext, TSourceType, TTargetType> value = null,
            Func<TemplateElemTransformationContext, TSourceType, IEnumerable<TTargetType>> values = null)
        {
            var item = new TemplateInstructionDefinition<TSourceType>
            {
                Name = name,
                ReturnType = typeof(TTargetType)
            };
            if (value != null)
            {
                item.IsListType = false;
                item.GetValue = (c, x) => value(c, x) as object;
            }

            if (values != null)
            {
                item.IsListType = true;
                item.GetValues = (c, x) => values(c, x).Cast<object>();
            }

            Items.Add(item);
        }

        public void AddCustomProp<TSourceType>(string props,
            Func<TemplateElemTransformationContext, TSourceType, object> value = null,
            Func<TemplateElemTransformationContext, TSourceType, IEnumerable<object>> values = null,
            Action<TemplateElemTransformationContext, TSourceType> execute = null)
        {
            props = props ?? "";
            foreach (var prop in props.Split(","))
            {
                var item = new TemplateInstructionDefinition<TSourceType>
                {
                    Name = prop?.Trim(),
                    ReturnType = null,
                    IsSimpleType = true,
                    GetValue = value,
                    GetValues = values
                };

                if (execute != null)
                    item.ExecuteCore = (c, x) => execute(c, (TSourceType)x);

                Items.Add(item);
            }
        }
    }

    public class TemplateTransformation
    {
        public TemplateProcessor Processor { get; set; }

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
