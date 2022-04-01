using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    public static class MojDefaultValuesConfigExtensions
    {
        public static IEnumerable<MojDefaultValueConfig> ForScenario(this MojDefaultValuesConfig config, params string[] scenarios)
        {
            if (!config.Is)
                yield break;

            scenarios ??= new string[] { null };
            foreach (var scenario in scenarios)
                foreach (var item in config.Items.Where(x => x.TargetScenario == scenario))
                    yield return item;
        }

        public static IEnumerable<MojDefaultValueConfig> WithAttr(this IEnumerable<MojDefaultValueConfig> items)
        {
            return items.Where(x => x.Attr != null);
        }

        public static IEnumerable<MojDefaultValueConfig> WithCommon(this IEnumerable<MojDefaultValueConfig> items)
        {
            return items.Where(x => x.CommonValue != null);
        }

        //public static string ToJsCodeString(this MojDefaultValueConfig config)
        //{
        //    if (config.Attr != null)
        //        return config.Attr.Args.First().ToJsCodeString();

        //    return MojenUtils.ToJsValue(arg.Value, arg.ValueType, parse: false, verbatim: arg.IsVerbatim);
        //}
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojDefaultValueConfig : MojBase
    {
        [DataMember]
        public string TargetScenario { get; set; }

        [DataMember]
        public MojDefaultValueAttr Attr { get; set; }

        [DataMember]
        public object Value { get; set; }

        [DataMember]
        public MojDefaultValueCommon? CommonValue { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojDefaultValuesConfig : MojBase
    {
        public static readonly MojDefaultValuesConfig None = new(false);

        public static readonly List<MojDefaultValueConfig> Empty = new(0);

        public MojDefaultValuesConfig()
            : this(true)
        { }

        MojDefaultValuesConfig(bool @is)
        {
            Is = @is;
            if (Is)
                Items = new List<MojDefaultValueConfig>();
        }

        [DataMember]
        public bool Is { get; private set; }

        [DataMember]
        public List<MojDefaultValueConfig> Items { get; private set; } = Empty;

        public void Set(object value, string scenario = null)
        {
            CheckIs();
            ClearScenario(scenario);
            Items.Add(new MojDefaultValueConfig { Value = value, TargetScenario = scenario });
        }

        public void Set(MojDefaultValueCommon value, string scenario = null)
        {
            CheckIs();
            ClearScenario(scenario);
            Items.Add(new MojDefaultValueConfig { CommonValue = value, TargetScenario = scenario });
        }

        public void Set(MojDefaultValueAttr attr, string scenario = null)
        {
            CheckIs();
            ClearScenario(scenario);
            Items.Add(new MojDefaultValueConfig { Attr = attr, TargetScenario = scenario });
        }

        void ClearScenario(string scenario)
        {
            var item = Items.FirstOrDefault(x => x.TargetScenario == scenario);
            if (item == null)
                return;

            Items.Remove(item);
        }

        void CheckIs()
        {
            if (!Is) throw new MojenException("The NULL object is immutable.");
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojDefaultValueAttr : MojAttr
    {
        public MojDefaultValueAttr(object value, Type type)
        {
            Name = "DefaultValue";
            Value = value;
            CArg("value", value, type);
        }

        [DataMember]
        public object Value { get; set; }
    }

    public abstract class MojAttrAccessor
    {
        public string Name { get; protected set; }
        public MojAttr Attr { get; set; }
    }

    public class MojPrecisionAttr : MojAttrAccessor
    {
        public MojPrecisionAttr()
        {
            Name = "Precision";
        }

        public int Precision
        {
            get { return (int)Attr.Args.GetValue("precision"); }
        }

        public int Scale
        {
            get { return (int)Attr.Args.GetValue("scale"); }
        }
    }

    public class MojAttrArgs : List<MojAttrArg>
    {
        public MojAttrArgs()
        { }

        public MojAttrArgs(IEnumerable<MojAttrArg> collection)
            : base(collection)
        { }

        public object GetValue(string name)
        {
            name = name.ToLower();
            return this.First(x => x.SearchName == name).Value;
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojAttr : MojBase
    {
        //public static MojAttr CreateRequired(string error = null)
        //{
        //    return new MojAttr("Required", 2).ErrorArg(error);
        //}

        public MojAttr()
        {
            Args = new MojAttrArgs();
        }

        public MojAttr(string name, int order)
            : this()
        {
            Name = name;
            Position = order;
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int Position { get; set; }

        [DataMember]
        public MojAttrArgs Args { get; private set; }

        // KABU TODO: REMOVE?
        //[DataMember]
        //public Attribute Attr { get; set; }

        public MojAttr Clone()
        {
            var clone = (MojAttr)MemberwiseClone();
            clone.Args = new MojAttrArgs(Args.Select(x => x.Clone()));

            return clone;
        }

        // KABU TODO: REMOVE?
        //void AssignFrom(MojAttr source)
        //{
        //    Name = source.Name;
        //    Position = source.Position;
        //    Attr = source.Attr;
        //    Args.AddRange(source.Args.Select(x => x.Clone()));
        //}

        /// <summary>
        /// Constructor string argument.
        /// </summary>        
        public MojAttr CSArg(string name, string value, bool verbatim = false)
        {
            return ArgCore(true, name, value, typeof(string), verbatim);
        }

        /// <summary>
        /// Constructor argument.
        /// </summary>
        public MojAttr CArg(string name, object value, Type type = null)
        {
            return ArgCore(true, name, value, type);
        }

        /// <summary>
        /// Property string argument.
        /// </summary>
        public MojAttr PSArg(string name, string value, bool verbatim = false)
        {
            return ArgCore(false, name, value, typeof(string), verbatim);
        }

        /// <summary>
        /// Property argument.
        /// </summary>
        public MojAttr PArg(string name, object value, Type type = null)
        {
            return ArgCore(false, name, value, type);
        }

        MojAttr ArgCore(bool isConstructor, string name, object value, Type type = null, bool verbatim = false)
        {
            Guard.ArgNotNullOrWhitespace(name, nameof(name));

            type ??= (value != null ? (Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType()) : null);

            Add(new MojAttrArg { IsConstructorArg = isConstructor, Name = name, Value = value, ValueType = type, IsVerbatim = verbatim });

            return this;
        }

        public MojAttr ErrorArg(string errorMessage)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
                Add(new MojAttrArg { Name = "ErrorMessage", Value = errorMessage, ValueType = typeof(string) });

            return this;
        }

        void Add(MojAttrArg arg)
        {
            arg.SearchName = arg.Name.ToLower();
            Args.Add(arg);
        }

        public override string ToString()
        {
            string result = "[" + Name;

            if (Args.Count != 0)
                result += "(" + Args.Select(x =>
                    (x.Name != null ? (x.Name + (x.IsConstructorArg ? ": " : " = ")) : ("")) +
                    x.ToCodeString(parse: false))
                    .Join(", ") + ")";

            return result + "]";
        }
    }
}