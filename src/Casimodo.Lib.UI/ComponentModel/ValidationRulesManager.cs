// Copyright (c) 2010 Kasimier Buchcik
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Casimodo.Lib.ComponentModel
{
    internal class AttributeRulesInfo
    {
        public PropertyInfo Prop;
        public ValidationAttribute[] Attributes;
    }

    internal class CustomRuleInfo
    {
        public int? ErrorCode;
        public Func<object, object> Validate;
        public string[] PropertyNames;
    }

    // KABU TODO: Add support for inherited validation rules.
    public class ValidationRulesManager
    {
        /// <summary>
        /// Adds a custom validation rule for the given type.
        /// </summary>
        public void Add(Type type, int errorCode, Func<object, object> validationCallback, params string[] propNames)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (errorCode < 0) throw new ArgumentOutOfRangeException(nameof(errorCode), "The given error code must be greater than or equal to zero.");
            if (propNames == null) throw new ArgumentNullException(nameof(propNames));
            if (validationCallback == null) throw new ArgumentNullException(nameof(validationCallback));

            if (!propNames.Any())
                return;

            foreach (var propName in propNames)
                ObservableObject.ThrowIfPropertyNotFound(type, propName);

            var rule = new CustomRuleInfo
            {
                ErrorCode = errorCode,
                Validate = validationCallback,
                PropertyNames = (string[])propNames.Clone()
            };

            foreach (var propName in propNames)
            {
                RegisterPropertyForValidation(type, propName);

                List<CustomRuleInfo> rules;
                if (_customRulesPerType.TryGetValue(type, out Dictionary<string, List<CustomRuleInfo>> rulesOfType))
                {
                    if (!rulesOfType.TryGetValue(propName, out rules))
                    {
                        rules = new List<CustomRuleInfo>();
                        rulesOfType.Add(propName, rules);
                    }
                }
                else
                {
                    rulesOfType = new Dictionary<string, List<CustomRuleInfo>>();
                    _customRulesPerType.Add(type, rulesOfType);

                    rules = new List<CustomRuleInfo>();
                    rulesOfType.Add(propName, rules);
                }

                rules.Add(rule);
            }
        }

        /// <summary>
        /// Adds a custom validation rule for the given instance.
        /// </summary>
        public void Add(ValidatingObservableObject instance, int errorCode, Func<object, object> validationCallback, params string[] propertyNames)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (errorCode < 0) throw new ArgumentOutOfRangeException(nameof(errorCode), "The given error code must be greater than or equal to zero.");
            if (propertyNames == null) throw new ArgumentNullException(nameof(propertyNames));
            if (validationCallback == null) throw new ArgumentNullException(nameof(validationCallback));

            if (!propertyNames.Any())
                return;

            Type type = instance.GetType();

            foreach (var propertyName in propertyNames)
                ObservableObject.ThrowIfPropertyNotFound(type, propertyName);

            var rule = new CustomRuleInfo
            {
                ErrorCode = errorCode,
                Validate = validationCallback,
                PropertyNames = (string[])propertyNames.Clone()
            };

            foreach (var propertyName in propertyNames)
            {
                RegisterPropertyForValidation(type, propertyName);

                if (!instance.CustomRulesPerInstance.TryGetValue(propertyName, out List<CustomRuleInfo> rules))
                {
                    rules = new List<CustomRuleInfo>();
                    instance.CustomRulesPerInstance.Add(propertyName, rules);
                }

                rules.Add(rule);
            }
        }

        public void Remove(ValidatingObservableObject instance, int errorCode, params string[] propertyNames)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (propertyNames == null) throw new ArgumentNullException(nameof(propertyNames));

            foreach (var propertyName in propertyNames)
                instance.RemoveValidationRulePerInstance(propertyName, errorCode);
        }

        /// <summary>
        /// Removes all custom per instance rules of the given instance.
        /// </summary>
        public void RemoveAll(ValidatingObservableObject instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            instance.RemoveAllValidationRulePerInstance();
        }

        /// <summary>
        /// Registers the given property for validation.
        /// </summary>
        internal void RegisterPropertyForValidation(Type type, string propertyName)
        {
            // TODO: Currently not thread-safe.

            // This is needed in order to fire validation when the property changes.
            if (!_propertiesHolder.TryGetValue(type, out Dictionary<string, object> props))
            {
                props = new Dictionary<string, object>
                {
                    { propertyName, null }
                };

                _propertiesHolder.Add(type, props);
            }
            else
            {
                if (!props.ContainsKey(propertyName))
                {
                    props.Add(propertyName, null);
                }
            }
        }

        internal List<CustomRuleInfo> GetCustomRulesPerType(Type type, string propertyName)
        {
            List<CustomRuleInfo> rules = null;
            if (_customRulesPerType.TryGetValue(type, out Dictionary<string, List<CustomRuleInfo>> rulesOfType))
            {
                rulesOfType.TryGetValue(propertyName, out rules);
            }

            return rules;
        }

        internal List<CustomRuleInfo> GetCustomRulesPerInstance(ValidatingObservableObject obj, string propertyName)
        {
            obj.CustomRulesPerInstance.TryGetValue(propertyName, out List<CustomRuleInfo> rules);

            return rules;
        }

        internal AttributeRulesInfo GetAttributeRules(Type type, string propertyName)
        {
            AttributeRulesInfo info;

            // Try to find matching type.
            if (_attributeValidationMap.TryGetValue(type, out Dictionary<string, AttributeRulesInfo> rules))
            {
                // Try to find matching property on the type.
                if (!rules.TryGetValue(propertyName, out info))
                {
                    // Add a new validation info for the property.
                    info = CreateAttributeRules(type, propertyName);
                    rules.Add(propertyName, info);
                }
            }
            else
            {
                // Add this type to the map.
                rules = new Dictionary<string, AttributeRulesInfo>();
                _attributeValidationMap.Add(type, rules);

                // Add a new validation info for the property.
                info = CreateAttributeRules(type, propertyName);
                rules.Add(propertyName, info);
            }

            return info;
        }

        /// <summary>
        /// Adds all rules expressed via ValidationAttribute(s) of the given type.
        /// </summary>
        public void Add(Type type)
        {
            // TODO: Currently not thread-safe.


            // Try to find matching type.
            if (!_propertiesHolder.TryGetValue(type, out Dictionary<string, object> props))
            {
                props = new Dictionary<string, object>();

                // Add all public properties which are annotated with at least one ValidationAttribute.
                foreach (var prop in type.GetProperties().Where(x => x.GetCustomAttributes(true).OfType<ValidationAttribute>().Any()))
                {
                    props.Add(prop.Name, null);
                }
                _propertiesHolder.Add(type, props);
            }
        }

        internal bool IsValidationNeeded(Type type, string propertyName)
        {
            return _propertiesHolder.TryGetValue(type, out Dictionary<string, object> props) && props.ContainsKey(propertyName);
        }

        internal IEnumerable<string> GetProperties(Type type)
        {
            if (_propertiesHolder.TryGetValue(type, out Dictionary<string, object> props))
            {
                return props.Keys.AsEnumerable<string>();
            }

            return null;
        }

        AttributeRulesInfo CreateAttributeRules(Type type, string propertyName)
        {
            var info = new AttributeRulesInfo();

            // Store the property.
            info.Prop = type.GetProperty(propertyName);
            if (info.Prop == null)
                throw new ArgumentException(
                    string.Format("No property with name '{0}' found in type '{1}'.", propertyName, type.Name),
                    nameof(propertyName));

            // Store all ValidationAttribute(s) of the specific property.
            info.Attributes =
                info.Prop
                    .GetCustomAttributes(typeof(ValidationAttribute), true)
                    .Cast<ValidationAttribute>()
                    .ToArray();

            return info;
        }

        internal int GetAttributeErrorCode(Type type)
        {
            if (!_attributeErrorCodes.TryGetValue(type, out int errorCode))
            {
                errorCode = (_attributeErrorCodes.Count + 1) * -1;
                _attributeErrorCodes.Add(type, errorCode);
            }

            return errorCode;
        }

        readonly Dictionary<Type, Dictionary<string, List<CustomRuleInfo>>> _customRulesPerType = new();

        /// <summary>
        /// Holds ValidationAttribute(s) of all types and properties.
        /// </summary>
        // TODO: Access is currently not thread-safe.
        static readonly Dictionary<Type, Dictionary<string, AttributeRulesInfo>> _attributeValidationMap =
            new();

        /// <summary>
        /// Holds the properties which are candidates for validation.
        /// </summary>
        static readonly Dictionary<Type, Dictionary<string, object>> _propertiesHolder = new();

        /// <summary>
        /// Holds the error codes of all ValidationAttribute(s) in use.
        /// The error codes are all of negative value in order to distinguish
        /// them from error codes of custom validation rules.
        /// </summary>
        // KABU TODO: Use thread-safe dictionary.
        static readonly Dictionary<Type, int> _attributeErrorCodes = new();
    }
}