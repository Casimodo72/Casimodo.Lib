using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

#nullable enable

namespace Casimodo.Lib.ComponentModel
{
    internal class ValidationAttributeRule : ValidationRule
    {
        internal ValidationAttributeRule(object errorCode, Type type,
            PropertyInfo propertyInfo, ValidationAttribute attribute)
            : base(errorCode, type)
        {
            if (propertyInfo == null) throw new ArgumentNullException(nameof(propertyInfo));
            if (attribute == null) throw new ArgumentNullException(nameof(attribute));

            Kind = ValidationSettingsKind.Attribute;

            PropertyInfo = propertyInfo;
            Attribute = attribute;

            PropertyNames = new[] { PropertyInfo.Name };
        }

        public PropertyInfo PropertyInfo { get; }
        public ValidationAttribute Attribute { get; }
    }
}