using System;

#nullable enable

namespace Casimodo.Lib.ComponentModel
{
    public abstract class ValidationRuleBuilder<TSelf, TModel>
        where TSelf : ValidationRuleBuilder<TSelf, TModel>
    {
        public ValidationRuleBuilder(object errorCode)
        {
            Rule = new(errorCode);
        }

        internal ValidationRule<TModel> Rule { get; }

        public TSelf Validate(Func<TModel, object> validate)
        {
            if (validate == null) throw new ArgumentNullException(nameof(validate));

            Rule.Validation = validate;

            return This();
        }

        public TSelf Properties(params string[] propertyNames)
        {
            Rule.PropertyNames = propertyNames;

            return This();
        }

        protected TSelf This()
        {
            return (TSelf)this;
        }
    }
}
