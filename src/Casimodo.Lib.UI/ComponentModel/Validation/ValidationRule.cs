using System;

#nullable enable

namespace Casimodo.Lib.ComponentModel
{
    internal sealed class ValidationRule<TModel> : ValidationRule
    {
        public ValidationRule(object errorCode)
            : base(errorCode, typeof(TModel))
        {
            IsCustom = true;
        }

        internal Func<TModel, object?> Validation
        {
            get => _validation;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));

                _validation = value;
                base.ObjectValidation = (obj) => _validation((TModel)obj);
            }
        }
        Func<TModel, object?> _validation = (m) => null;
    }

    internal class ValidationRule
    {
        internal ValidationRule(object errorCode, Type type)
        {
            if (errorCode == null) throw new ArgumentNullException(nameof(errorCode));
            if (type == null) throw new ArgumentNullException(nameof(type));

            ErrorCode = errorCode;
            Type = type;
        }

        public ValidationSettingsKind Kind { get; protected set; } = ValidationSettingsKind.Custom;
        public Type Type { get; }
        public object ErrorCode { get; }
        public bool IsCustom { get; protected set; }
        internal Func<object, object?> ObjectValidation { get; set; } = (m) => null;

        public object? Validate(object modelObj)
        {
            if (modelObj == null) throw new ArgumentNullException(nameof(modelObj));

            if (modelObj.GetType() != Type)
            {
                throw new InvalidOperationException($"Validation: Incorrect model type '{modelObj.GetType()}'. " +
                    $"Expected was a model of type '{Type}'.");
            }

            return ObjectValidation(modelObj);
        }

        public string[] PropertyNames { get; set; } = Array.Empty<string>();
    }

    internal enum ValidationSettingsKind
    {
        Custom,
        Attribute
    }
}