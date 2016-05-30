using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class MiaTypeOperationsBuilder
    {
        public MojenApp App { get; set; }

        public void Init(MojenApp app)
        {
            App = app;
        }

        public MojType Type { get; set; }

        MiaTypeTriggerConfig _trigger;

        public MiaTypeOperationsBuilder OnCreate()
        {
            return On(MiaTypeTriggerEventKind.Create);
        }

        public MiaTypeOperationsBuilder OnUpdate()
        {
            return On(MiaTypeTriggerEventKind.Update);
        }

        public MiaTypeOperationsBuilder OnDelete()
        {
            return On(MiaTypeTriggerEventKind.Delete);
        }

        public MiaTypeOperationsBuilder OnPropChanged(MojFormedType referenceProp)
        {
            // We use the foreign key prop if a formed type was given.
            return OnPropChanged(referenceProp.FormedNavigationFrom.Last.SourceProp);
        }

        public MiaTypeOperationsBuilder OnPropChanged(MojProp prop)
        {
            prop = ToEntity(prop);
            // Must be a native prop.
            CheckIsNative(prop);

            _trigger = new MiaTypeTriggerConfig
            {
                ContextType = Type,
                Event = MiaTypeTriggerEventKind.PropChanged,
                ContextProp = prop
            };

            Type.Triggers.Add(_trigger);
            return this;
        }

        public MiaTypeOperationsBuilder On(MiaTypeTriggerEventKind eve)
        {
            _trigger = new MiaTypeTriggerConfig
            {
                ContextType = Type,
                Event = eve
            };

            Type.Triggers.Add(_trigger);
            return this;
        }

        public MiaTypeOperationsBuilder Do(MojCrudOp operation, MojType type = null, bool many = false)
        {
            _trigger.CrudOp = operation;

            if (type != null)
            {
                if (many)
                    Many(type);
                else
                    Single(type);
            }

            return this;
        }

        public MiaTypeOperationsBuilder NoRepository()
        {
            _trigger.ForScenario &= ~MiaTriggerScenario.Repository;
            return this;
        }

        public MiaTypeOperationsBuilder Create(MojType type = null, bool many = false)
        {
            return Do(MojCrudOp.Create, type, many);
        }

        public MiaTypeOperationsBuilder Update(MojType type = null, bool many = false)
        {
            return Do(MojCrudOp.Update, type, many);
        }

        public MiaTypeOperationsBuilder Delete(MojType type = null, bool many = false)
        {
            return Do(MojCrudOp.Delete, type, many);
        }

        public MiaTypeOperationsBuilder Single(MojType type)
        {
            type = UseEntity(type);
            _trigger.TargetType = type;
            _trigger.Multiplicity = MojMultiplicity.One;
            return this;
        }

        public MiaTypeOperationsBuilder Many(MojType type)
        {
            type = UseEntity(type);
            _trigger.TargetType = type;
            _trigger.Multiplicity = MojMultiplicity.Many;
            return this;
        }

        public MiaTypeOperationsBuilder Name(string operationName)
        {
            _trigger.Name = operationName;
            return this;
        }

        // Object mapping ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public MiaTypeOperationsBuilder UseMapFunc(string mappingFunctionName)
        {
            Guard.ArgNotNullOrWhitespace(mappingFunctionName, nameof(mappingFunctionName));

            _trigger.Operations.MappingFunctionName = mappingFunctionName;
            return this;
        }

        public MiaTypeOperationsBuilder UseFactoryFunc(string factoryFunctionCall)
        {
            Guard.ArgNotNullOrWhitespace(factoryFunctionCall, nameof(factoryFunctionCall));

            _trigger.Operations.FactoryFunctionCall = factoryFunctionCall;
            return this;
        }

        void CheckIsAccessible(MojProp prop)
        {
            if (!Type.IsAccessibleFromThis(prop))
                throw new MojenException($"Property '{prop.Name}' neither exist on context type '{Type.ClassName}' nor can be navigated to.");
        }

        void CheckIsNative(MojProp prop)
        {
            if (!Type.IsNativeProp(prop) || prop.FormedNavigationTo.Is)
                throw new MojenException($"Property '{prop.Name}' does not exist on context type '{Type.ClassName}'.");
        }

        void CheckIsForeignButAccessible(MojProp prop)
        {
            if (!prop.FormedNavigationTo.Is) // Type.IsNativeProp(prop))
                throw new MojenException($"Property '{prop.FormedTargetPath}' must be a foreign property.");

            if (!Type.IsAccessibleFromThis(prop))
                throw new MojenException($"Property '{prop.FormedTargetPath}' cannot be navigated to from context type '{Type.ClassName}'.");
        }

        public MiaTypeOperationsBuilder Set(MojProp prop, MojProp value)
        {
            Guard.ArgNotNull(prop, nameof(prop));
            Guard.ArgNotNull(value, nameof(value));

            prop = ToEntity(prop);
            CheckIsNative(prop);

            value = ToEntity(value);
            CheckIsForeignButAccessible(value);

            var setter = new MiaPropSetterConfig
            {
                Target = prop,
                Source = value,
                IsNativeSource = false
            };

            _trigger.Operations.Items.Add(setter);

            return this;
        }

        public MiaTypeOperationsBuilder Map(MojProp source, MojProp target = null)
        {
            Guard.ArgNotNull(source, nameof(source));

            source = ToEntity(source);
            CheckIsAccessible(source);

            if (target == null)
            {
                target = _trigger.TargetType.FindProp(source.Name);
                if (target == null)
                    throw new MojenException($"Property '{source.Name}' does not exist on target type '{_trigger.TargetType.ClassName}'.");
            }
            else
            {
                target = ToEntity(target);
            }

            if (!_trigger.TargetType.IsNativeProp(target))
                throw new MojenException($"Property '{target.Name}' does not exist on source type '{_trigger.TargetType}'.");

            _trigger.Operations.Items.Add(new MiaPropSetterConfig
            {
                Target = target,
                Source = source,
                IsNativeSource = Type.IsNativeProp(source)
            });
            return this;
        }

        MojType UseEntity(MojType type)
        {
            return type.RequiredStore;
        }

        MojProp ToEntity(MojProp prop)
        {
            if (prop == null) return null;

            if (prop.FormedNavigationFrom.Is)
                prop = prop.FormedNavigationFrom.ToRootEntityProp();

            prop = prop.RequiredStore;

            return prop;
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public void Build()
        {
            if (Type.Store != null)
                Type.Store.Triggers.AddRange(Type.Triggers);
        }
    }
}
