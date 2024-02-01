using System;
using System.Collections.Generic;
using System.Linq;
#nullable enable

namespace Casimodo.Lib.Templates
{
    public class TemplateDataContainer
    {
        public TemplateDataContainer Self { get { return this; } }
        readonly List<TemplateDataPropAccessor> _properties = [];

        public void AddProp<T>(string name, T? instance = default)
        {
            var prop = new TemplateDataPropAccessor(name, typeof(T), instance);
            _properties.Add(prop);
        }

        public void AddProp(string name, Type type, object? instance = null)
        {
            _properties.Add(new TemplateDataPropAccessor(name, type, instance));
        }

        public void RemoveProp(string name)
        {
            Guard.ArgNotNullOrWhitespace(name);

            var prop = FindPropAccessor(name, null);
            if (prop == null)
                return;

            _properties.Remove(prop);
        }

        public void SetProp<T>(string name, T? instance)
            where T : class
        {
            GetPropAccessor(name, typeof(T)).ValueObject = instance;
        }

        public void SetProp<T>(T? instance)
            where T : class
        {
            GetPropAccessor(null, typeof(T)).ValueObject = instance;
        }

        public T? GetProp<T>(string name)
        {
            var prop = GetPropAccessor(name, typeof(T));

            return (T?)prop.ValueObject;
        }

        public T? GetProp<T>()
        {
            var prop = GetPropAccessor(null, typeof(T));

            return (T?)prop.ValueObject;
        }

        public T GetRequiredProp<T>()
            where T : class
        {
            var prop = GetPropAccessor(null, typeof(T));

            if (prop.ValueObject == null)
                throw new TemplateException($"The value of the property is null (name: '{prop!.Name}', type: '{typeof(T).Name}').");

            return (T)prop!.ValueObject;
        }

        public T GetRequiredProp<T>(string name)
           where T : class
        {
            var prop = GetPropAccessor(name, typeof(T));

            if (prop.ValueObject == null)
                throw new TemplateException($"The value of the property is null (name: '{prop!.Name}', type: '{typeof(T).Name}').");

            return (T)prop!.ValueObject;
        }

        internal object? GetPropValueObject(string name)
        {
            var prop = GetPropAccessor(name, null);

            return prop?.ValueObject;
        }

        public IEnumerable<TemplateDataPropAccessor> GetPropAccessors()
        {
            return _properties;
        }

        internal TemplateDataPropAccessor? FindPropAccessor(string? name, Type? type)
        {
            if (string.IsNullOrWhiteSpace(name) && type == null)
                throw new ArgumentException("At least one of @name or @type must be specified.");

            var props = _properties
                .Where(x =>
                    (name == null || x.Name == name) &&
                    (type == null || x.Type == type))
                .ToArray();

            if (props.Length > 1)
            {
                throw new TemplateException(
                    $"Multiple data properties with name '{name ?? "(none)"}' " +
                    $"of type '{type?.Name ?? "(none)"}' found.");
            }

            var prop = props.FirstOrDefault();

            return prop;
        }

        private TemplateDataPropAccessor GetPropAccessor(string? name, Type? type, bool createIfMissing = false)
        {
            var prop = FindPropAccessor(name, type);

            if (prop == null && createIfMissing)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new TemplateException($"Cannot add a property of type '{type!.Name}': No property name was provided.");

                if (type == null)
                    throw new TemplateException($"Cannot add a property '{name}': No property type was provided.");

                AddProp(name, type);

                return GetPropAccessor(name, type);
            }

            if (prop == null)
                throw new TemplateException($"Data property '{name}' of type '{(type != null ? type.Name : "[not specified]")}' not found.");

            return prop;
        }
    }

    public class TemplateDataPropAccessor(string name, Type type, object? instanceObject)
    {
        public Guid Guid { get; } = Guid.NewGuid();
        public string Name { get; } = name;
        public Type Type { get; } = type;

        public object? ValueObject { get; set; } = instanceObject;
    }

    public interface ITemplateInstructionResolver
    {
        TemplateInstructionDefinition? ResolveInstruction(Type sourceType, string propName);
    }

    public class TemplateInstructions : ITemplateInstructionResolver
    {
        public TemplateInstructionDefinition? ResolveInstruction(Type sourceType, string propName)
        {
            return Instructions.FirstOrDefault(x => x.Name == propName && x.SourceType.IsAssignableFrom(sourceType));
        }

        public TemplateFunctionDefinition? ResolveFunction(string funcName)
        {
            return Functions.FirstOrDefault(x => x.Name == funcName);
        }

        public List<TemplateInstructionDefinition> Instructions { get; set; } = [];
        public List<TemplateFunctionDefinition> Functions { get; set; } = [];

        public void Prop<TSourceType, TTargetType>(string names,
            Func<TemplateExpressionContext, TSourceType, TTargetType> value)
        {
            AddInstruction<TSourceType, TTargetType>(names, valueGetter: value);
        }

        public void CollectionProp<TSourceType, TTargetType>(string names,
            Func<TemplateExpressionContext, TSourceType, IEnumerable<TTargetType>> values)
        {
            AddInstruction<TSourceType, TTargetType>(names, listValueGetter: values);
        }

        void AddInstruction<TSourceType, TTargetType>(string names,
            Func<TemplateExpressionContext, TSourceType, TTargetType>? valueGetter = null,
            Func<TemplateExpressionContext, TSourceType, IEnumerable<TTargetType>>? listValueGetter = null)
        {
            CheckComplexSourceType(typeof(TSourceType));
            Guard.ArgNotNullOrWhitespace(names);
            Guard.ArgMutuallyExclusive(valueGetter, listValueGetter);
            Guard.ArgOneNotNull(valueGetter, listValueGetter);

            var isReturnTypeSimple = TypeHelper.IsSimple(typeof(TTargetType));

            if (listValueGetter != null && isReturnTypeSimple)
                throw new TemplateException(
                    "Custom template expression instructions which " +
                    "return multiple values must return values of complex type. But the specified " +
                    $"return type '{typeof(TTargetType).Name}' is a simple type.");

            names ??= "";
            foreach (var name in names.Split(',').Select(x => x.Trim()))
            {
                CheckName(name);

                var item = new TemplateInstructionDefinition<TSourceType>(typeof(TTargetType))
                {
                    Name = name,
                    IsReturnTypeSimple = isReturnTypeSimple
                };

                if (valueGetter != null)
                {
                    item.IsReturnTypeList = false;
                    item.ValueGetter = (c, x) => valueGetter(c, x) as object;
                }
                else if (listValueGetter != null)
                {
                    item.IsReturnTypeList = true;
                    item.ListValueGetter = (c, x) => listValueGetter(c, x).Cast<object>();
                }

                Instructions.Add(item);
            }
        }

        public void Action<TSourceType>(string name,
            Action<TemplateExpressionContext, TSourceType>? execute = null)
        {
            CheckComplexSourceType(typeof(TSourceType));
            CheckName(name);
            Guard.ArgNotNull(execute);

            var item = new TemplateInstructionDefinition<TSourceType>(AstTypeInfo.NoType)
            {
                Name = name.Trim(),
                IsReturnTypeSimple = true,
                ExecuteCore = (c, x) => execute(c, (TSourceType)x)
            };

            Instructions.Add(item);
        }

        static void CheckName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Empty instruction name.");
            if (name.Contains('.'))
                throw new ArgumentException("The instruction name must not contain any dot characters.");
        }

        public static void CheckComplexSourceType(Type type)
        {
            if (TypeHelper.IsSimple(type))
                throw new TemplateException(
                    "Custom template expression instructions must have a complex source type. " +
                    $"But the specified source type '{type.Name}' is a simple type.");
        }
    }
}
