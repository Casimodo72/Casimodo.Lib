// Copyright (c) 2010 Kasimier Buchcik
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

#nullable enable

namespace Casimodo.Lib.ComponentModel
{
    internal sealed class TypeRulesContainer : Dictionary<string, List<ValidationRule>>
    {
    }

    /// <summary>
    /// Validation rules manager.
    /// </summary>
    public class ValidationRulesManager
    {
        /// <summary>
        /// Holds all properties registered for validation.
        /// </summary>
        static readonly Dictionary<Type, Dictionary<string, object?>> PropertyRegistry = new();

        readonly Dictionary<Type, TypeRulesContainer> _rulesPerType = new();

        public void AddRule<TModel>(object errorCode, Action<CustomValidationRulesBuilder<TModel>> build)
        {
            if (errorCode == null) throw new ArgumentNullException(nameof(errorCode));
            if (build == null) throw new ArgumentNullException(nameof(build));

            var builder = new CustomValidationRulesBuilder<TModel>(errorCode);

            build(builder);

            Add(builder.Rule);
        }

        /// <summary>
        /// Adds all rules expressed via ValidationAttributes of the given type.
        /// </summary>        
        public void Add(Type type)
        {
            // TODO: Use AddAttributeValidation instead of Add in consumer code.

            AddAttributeRules(type);
        }

        /// <summary>
        /// Adds all rules expressed via ValidationAttributes of the given type.
        /// </summary>
        public void AddAttributeRules(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            Dictionary<string, List<ValidationRule>>? rulesOfType = null;

            foreach (var item in type.GetProperties()
                .Select(prop => new
                {
                    Prop = prop,
                    Attributes = (ValidationAttribute[])prop.GetCustomAttributes(typeof(ValidationAttribute), true)
                })
                .Where(x => x.Attributes.Length > 0))
            {
                RegisterProperty(type, item.Prop.Name);

                rulesOfType ??= GetOrCreateRulesForType(type);

                var rules = GetOrCreateRulesForTypeProperty(rulesOfType, item.Prop.Name);

                foreach (var attribute in item.Attributes)
                {
                    var rule = new ValidationAttributeRule(
                        $"{item.Prop.Name}-{attribute.TypeId}",
                        type,
                        item.Prop,
                        attribute
                    );

                    rules.Add(rule);
                }
            }
        }

        /// <summary>
        /// Adds a custom validation rule for the given type.
        /// </summary>
        public void Add(Type type, object errorCode, Func<object, object> validationCallback, params string[] propNames)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (errorCode == null) throw new ArgumentNullException(nameof(errorCode));
            if (propNames == null) throw new ArgumentNullException(nameof(propNames));
            if (validationCallback == null) throw new ArgumentNullException(nameof(validationCallback));

        }

        void Add(ValidationRule rule)
        {
            if (!rule.PropertyNames.Any())
                return;

            foreach (var propName in rule.PropertyNames)
                ObservableObject.ThrowIfPropertyNotFound(rule.Type, propName);

            var rulesOfType = GetOrCreateRulesForType(rule.Type);

            foreach (var propName in rule.PropertyNames)
            {
                RegisterProperty(rule.Type, propName);

                var rules = GetOrCreateRulesForTypeProperty(rulesOfType, propName);

                rules.Add(rule);
            }
        }

        // TODO: Use rule builder.
        /// <summary>
        /// Adds a custom validation rule for the given instance.
        /// </summary>
        public void Add(ValidatingObservableObject instance, object errorCode, Func<object, object> validationCallback, params string[] propertyNames)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (errorCode == null) throw new ArgumentNullException(nameof(errorCode));
            if (propertyNames == null) throw new ArgumentNullException(nameof(propertyNames));
            if (validationCallback == null) throw new ArgumentNullException(nameof(validationCallback));

            if (!propertyNames.Any())
                return;

            var rule = new ValidationRule(errorCode, instance.GetType())
            {
                ObjectValidation = validationCallback,
                PropertyNames = (string[])propertyNames.Clone()
            };

            foreach (var propertyName in rule.PropertyNames)
                ObservableObject.ThrowIfPropertyNotFound(rule.Type, propertyName);

            foreach (var propertyName in propertyNames)
            {
                RegisterProperty(rule.Type, propertyName);

                if (!instance.CustomRulesPerInstance.TryGetValue(propertyName, out var rules))
                {
                    rules = new List<ValidationRule>();
                    instance.CustomRulesPerInstance.Add(propertyName, rules);
                }

                rules.Add(rule);
            }
        }

        List<ValidationRule> GetOrCreateRulesForTypeProperty(
           Dictionary<string, List<ValidationRule>> rulesOfType,
           string propertyName)
        {
            if (!rulesOfType.TryGetValue(propertyName, out List<ValidationRule>? rules))
            {
                rules = new List<ValidationRule>();
                rulesOfType.Add(propertyName, rules);
            }

            return rules;
        }

        TypeRulesContainer GetOrCreateRulesForType(Type type)
        {
            if (!_rulesPerType.TryGetValue(type, out TypeRulesContainer? rulesOfType))
            {
                rulesOfType = new TypeRulesContainer();
                _rulesPerType.Add(type, rulesOfType);
            }

            return rulesOfType;
        }

        public void RemoveRule(ValidatingObservableObject instance, object errorCode, params string[] propertyNames)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (errorCode == null) throw new ArgumentNullException(nameof(errorCode));
            if (propertyNames == null) throw new ArgumentNullException(nameof(propertyNames));

            foreach (var propertyName in propertyNames)
            {
                instance.RemoveValidationRulePerInstance(propertyName, errorCode);
            }
        }

        /// <summary>
        /// Removes all custom per instance rules of the given instance.
        /// </summary>
        public void RemoveAllRules(ValidatingObservableObject instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            instance.RemoveAllValidationRulePerInstance();
        }

        /// <summary>
        /// Registers the given property for validation.
        /// </summary>
        internal void RegisterProperty(Type type, string propertyName)
        {
            if (!PropertyRegistry.TryGetValue(type, out var props))
            {
                props = new Dictionary<string, object?>
                {
                    { propertyName, null }
                };

                PropertyRegistry.Add(type, props);
            }
            else
            {
                if (!props.ContainsKey(propertyName))
                {
                    props.Add(propertyName, null);
                }
            }
        }

        internal IEnumerable<ValidationRule> GetRules(Type type, string propertyName)
        {
            return _rulesPerType.TryGetValue(type, out var rulesOfType) &&
                rulesOfType!.TryGetValue(propertyName, out var rules)
                ? rules
                : Array.Empty<ValidationRule>();
        }

        internal IEnumerable<ValidationRule> GetRules(
            ValidatingObservableObject validatable, string propertyName)
        {
            return validatable.CustomRulesPerInstance.TryGetValue(propertyName, out var rules)
                ? rules
                : Array.Empty<ValidationRule>();
        }

        internal bool IsRegisteredProperty(Type type, string propertyName)
        {
            return PropertyRegistry.TryGetValue(type, out Dictionary<string, object?>? props) && props.ContainsKey(propertyName);
        }

        internal IEnumerable<string>? GetRegisteredPropertyNames(Type type)
        {
            return PropertyRegistry.TryGetValue(type, out Dictionary<string, object?>? props)
                ? props!.Keys
                : null;
        }
    }
}