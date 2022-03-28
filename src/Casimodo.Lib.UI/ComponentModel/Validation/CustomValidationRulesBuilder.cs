#nullable enable

namespace Casimodo.Lib.ComponentModel
{
    public sealed class CustomValidationRulesBuilder<TModel> :
        ValidationRuleBuilder<CustomValidationRulesBuilder<TModel>, TModel>
    {
        internal CustomValidationRulesBuilder(object errorCode)
            : base(errorCode)
        {
        }
    }
}