using System.Runtime.Serialization;

namespace Casimodo.Mojen
{
    // TODO: Use dedicated class for defaults.
    public class MojValueSetProp : MojBase
    {
        [DataMember]
        public int? MappingIndex { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public object Value
        {
            get
            {
                if (ProvideValue != null)
                    throw new MojenException($"Prop named '{Name}' can get its value only via the provider.");

                return _value;
            }
            set
            {
                if (ProvideValue != null)
                    throw new MojenException($"Prop named '{Name}' cannot set its value because it uses a value provider.");

                _value = value;
            }

        }
        object _value;

        public Func<MojValueSet, object> ProvideValue;

        internal object GetValue(MojValueSet valueSet)
        {
            Guard.ArgNotNull(valueSet, nameof(valueSet));

            return ProvideValue != null
                ? ProvideValue(valueSet)
                : Value;
        }

        internal void AssignValueFrom(MojValueSetProp other)
        {
            if (other.ProvideValue != null)
                ProvideValue = other.ProvideValue;
            else
                Value = other.Value;
        }

        public void Build(MojValueSet valueSet)
        {
            if (ProvideValue != null)
            {
                var val = ProvideValue(valueSet);
                ProvideValue = null;
                Value = val;
            }
        }

        /// <summary>
        /// Only used for file paths.
        /// </summary>
        [DataMember]
        public string Kind { get; set; }

        public string ValueToString()
        {
            return Value?.ToString();
        }
    }
}