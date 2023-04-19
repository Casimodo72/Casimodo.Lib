using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Templates
{
    public class TemplateDataContainer
    {
        public TemplateDataContainer Self { get { return this; } }
        readonly List<TemplateDataPropAccessor> _properties = new List<TemplateDataPropAccessor>();

        public void AddProp<T>(string name, T instance = default(T))
            where T : new()
        {
            _properties.Add(new TemplateDataPropAccessor
            {
                Type = typeof(T),
                Name = name,
                InstanceObject = instance
            });
        }

        public void AddProp(Type type, string name, object instance = null)
        {
            _properties.Add(new TemplateDataPropAccessor
            {
                Type = type,
                Name = name,
                InstanceObject = instance
            });
        }

        public void RemoveProp(string name)
        {
            var prop = GetPropAccessor(name, required: false);
            if (prop == null)
                return;

            _properties.Remove(prop);
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

            if (instance != null && prop.Type != null && !prop.Type.IsAssignableFrom(instance.GetType()))
                throw new TemplateException($"Incorrent property type '{instance.GetType().Name}'. Expected was a type assignable to type '{prop.Type}'.");

            prop.InstanceObject = instance;
        }

        public T Prop<T>(string name)

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

        public object Prop(string name)
        {
            var prop = GetPropAccessor(name);

            return prop.InstanceObject;
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
                throw new TemplateException($"Data property '{name}' not found.");

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
    }

    public class TemplateDataPropAccessor
    {
        public TemplateDataPropAccessor()
        {
            Guid = Guid.NewGuid();
        }

        public Guid Guid { get; set; }
        public string Name { get; set; }
        public Type Type { get; set; }

        public object InstanceObject { get; set; }
    }

    public interface ITemplateInstructionResolver
    {
        TemplateInstructionDefinition ResolveInstruction(Type sourceType, string propName);
    }

    public class TemplateInstructions : ITemplateInstructionResolver
    {
        public TemplateInstructionDefinition ResolveInstruction(Type sourceType, string propName)
        {
            return Instructions.FirstOrDefault(x => x.Name == propName && x.SourceType.IsAssignableFrom(sourceType));
        }

        public TemplateFunctionDefinition ResolveFunction(string funcName)
        {
            return Functions.FirstOrDefault(x => x.Name == funcName);
        }

        public List<TemplateInstructionDefinition> Instructions { get; set; } = new List<TemplateInstructionDefinition>();
        public List<TemplateFunctionDefinition> Functions { get; set; } = new List<TemplateFunctionDefinition>();

        public void Prop<TSourceType, TTargetType>(string names,
            Func<TemplateExpressionContext, TSourceType, TTargetType> value)
        {
            AddInstruction<TSourceType, TTargetType>(names, value: value);
        }

        public void CollectionProp<TSourceType, TTargetType>(string names,
            Func<TemplateExpressionContext, TSourceType, IEnumerable<TTargetType>> values)
        {
            AddInstruction<TSourceType, TTargetType>(names, values: values);
        }

        void AddInstruction<TSourceType, TTargetType>(string names,
            Func<TemplateExpressionContext, TSourceType, TTargetType> value = null,
            Func<TemplateExpressionContext, TSourceType, IEnumerable<TTargetType>> values = null)
        {
            CheckComplexSourceType(typeof(TSourceType));
            Guard.ArgNotNullOrWhitespace(names, nameof(names));
            Guard.ArgMutuallyExclusive(value, values, nameof(value), nameof(values));
            Guard.ArgOneNotNull(value, values, nameof(value), nameof(values));

            var isReturnTypeSimple = TypeHelper.IsSimple(typeof(TTargetType));

            if (values != null && isReturnTypeSimple)
                throw new TemplateException(
                    "Custom template expression instructions which " +
                    "return multiple values must return values of complex type. But the specified " +
                    $"return type '{typeof(TTargetType).Name}' is a simple type.");

            names = names ?? "";
            foreach (var name in names.Split(',').Select(x => x.Trim()))
            {
                CheckName(name);

                var item = new TemplateInstructionDefinition<TSourceType>
                {
                    Name = name,
                    ReturnType = typeof(TTargetType),
                    IsReturnTypeSimple = isReturnTypeSimple
                };

                if (value != null)
                {
                    item.IsReturnTypeList = false;
                    item.GetValue = (c, x) => value(c, x) as object;
                }
                else if (values != null)
                {
                    item.IsReturnTypeList = true;
                    item.GetValues = (c, x) => values(c, x).Cast<object>();
                }

                Instructions.Add(item);
            }
        }

        public void Action<TSourceType>(string name,
            Action<TemplateExpressionContext, TSourceType> execute = null)
        {
            CheckComplexSourceType(typeof(TSourceType));
            CheckName(name);
            Guard.ArgNotNull(execute, nameof(execute));

            var item = new TemplateInstructionDefinition<TSourceType>
            {
                Name = name?.Trim(),
                ReturnType = null,
                IsReturnTypeSimple = true
            };

            item.ExecuteCore = (c, x) => execute(c, (TSourceType)x);

            Instructions.Add(item);
        }


        // KABU TODO: REMOVE? Intended for function "EnableArea", but currently - maybe as a workaround -
        //   I'm using expressions like "Something-Area" with a registered executing instruction handler instead.
        void AddGlobalFunc<TSourceType>(string name, Action<TemplateExpressionContext> execute = null)
        {
            var func = new TemplateFunctionDefinition
            {
                Name = name
            };

            func.ExecuteCore = (c, x) => execute(c);

            Functions.Add(func);
        }

        static void CheckName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Empty instruction name.");
            if (name.Contains("."))
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

    public abstract class TemplateCoreContext
    {
        public Action<TemplateExpressionContext> TemplateExpressionContextConfiguring { get; set; }

        public TemplateDataContainer Data { get; set; }

        /// <summary>
        /// Additional properties (and values) provided by external means. E.g. by the FlexEmailDocumentTemplate itself.
        /// </summary>
        public TemplateExternalPropertiesContainer ExternalPropertiesContainer { get; set; }
            = new TemplateExternalPropertiesContainer();

        /// <summary>
        /// Processes properties defined by the template itself.
        /// </summary>
        public virtual void ProcessCustomProperty(TemplateExpressionContext context)
        {
            var propName = context.CurrentInstruction.Name;

            var prop = ExternalPropertiesContainer.Items.FirstOrDefault(x => x.Name == propName);
            if (prop == null)
                throw new TemplateException($"External property '{propName}' not found.");

            context.Processor.SetText(prop.Value);

            context.Processor.IsMatch = true;
        }

        readonly ConcurrentDictionary<Guid, TemplateInstructionDefinition> _instructionDefinitionsCache =
           new ConcurrentDictionary<Guid, TemplateInstructionDefinition>();

        /// <summary>
        /// Resolves data root properties and external properties.
        /// </summary>
        public TemplateInstructionDefinition ResolveInstruction(Type sourceType, string propName)
        {
            if (sourceType == typeof(TemplateExternalPropertiesContainer))
            {
                var prop = ExternalPropertiesContainer.Items.FirstOrDefault(x => x.Name == propName);
                if (prop == null)
                    return null;

                if (_instructionDefinitionsCache.TryGetValue(prop.Guid, out TemplateInstructionDefinition definition))
                    return definition;

                definition = new TemplateInstructionDefinition<TemplateExternalPropertiesContainer>
                {
                    Name = prop.Name,
                    ReturnType = typeof(string),
                    IsReturnTypeSimple = true
                };

                definition.ExecuteCore = (c, x) => ProcessCustomProperty(c);

                _instructionDefinitionsCache.TryAdd(prop.Guid, definition);

                return definition;
            }
            else if (sourceType == typeof(TemplateDataContainer))
            {
                var prop = Data.GetPropAccessor(propName, required: false);
                if (prop == null)
                    return null;

                if (_instructionDefinitionsCache.TryGetValue(prop.Guid, out TemplateInstructionDefinition def))
                    return def;

                var definition = new TemplateInstructionDefinition<TemplateDataContainer>
                {
                    Name = prop.Name,
                    ReturnType = prop.Type
                };

                definition.GetValue = (c, x) => x.Prop(c.CurrentInstruction.Name);

                _instructionDefinitionsCache.TryAdd(prop.Guid, definition);

                return definition;
            }

            return null;
        }

        public abstract TemplateExpressionParser GetExpressionParser();

        public virtual TemplateExpressionContext CreateExpressionContext(TemplateProcessor templateProcessor)
        {
            var context = new TemplateExpressionContext();
            context.CoreContext = this;
            context.DataContainer = Data;
            context.Processor = templateProcessor;

            Data.Prop<TemplateEnvContainer>().Context = context;

            return context;
        }
    }
}
