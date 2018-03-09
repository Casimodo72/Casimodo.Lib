using System;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    public static class MojAttrExtensions
    {
        public static string ToCodeString(this MojAttrArg arg, bool parse = true)
        {
            return MojenUtils.ToCsValue(arg.Value, arg.ValueType, parse, verbatim: arg.IsVerbatim);
        }
    }

    public static class MojAttrJsExtensions
    {
        public static string ToJsCodeString(this MojAttrArg arg)
        {
            return MojenUtils.ToJsValue(arg.Value, arg.ValueType, parse: false, verbatim: arg.IsVerbatim);
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojAttrArg
    {
        [DataMember]
        public string Name { get; set; }

        public string SearchName { get; set; }

        [DataMember]
        public bool IsConstructorArg { get; set; }

        bool IsStringOrGuid
        {
            get { return ValueType == typeof(string) || ValueType == typeof(Guid); }
        }

        [DataMember]
        public bool IsVerbatim { get; set; }

        [DataMember]
        public object Value { get; set; }

        public Type ValueType { get; set; }

        [DataMember]
        string _valueTypeName;

        public MojAttrArg Clone()
        {
            var clone = (MojAttrArg)MemberwiseClone();

            return clone;
        }

        // KABU TODO: REMOVE?
        //void AssignFrom(MojAttrArg source)
        //{
        //    Name = source.Name;
        //    IsConstructorArg = source.IsConstructorArg;
        //    Value = source.Value;
        //    ValueType = source.ValueType;
        //    IsVerbatim = source.IsVerbatim;
        //}

        // KABU TODO: REMOVE
        //public override string ToString()
        //{
        //    var result = "";
        //    result += (ValueType == typeof(string)) && IsVerbatim ? "@" : "";
        //    result += IsStringOrGuid ? ("\"" + Value + "\"") : Value;

        //    return result;
        //}

        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            _valueTypeName = ValueType != null ? ValueType.FullName : null;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (_valueTypeName != null)
                ValueType = Type.GetType(_valueTypeName);
            if (Name != null)
                SearchName = Name.ToLower();
        }
    }
}