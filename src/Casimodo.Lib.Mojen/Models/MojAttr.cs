﻿using System;
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

            scenarios = scenarios ?? new string[] { null };
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
        public static readonly MojDefaultValuesConfig None = new MojDefaultValuesConfig(false);

        public static readonly List<MojDefaultValueConfig> Empty = new List<MojDefaultValueConfig>(0);

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

        public void Add(object value, string scenario = null)
        {
            CheckIs();
            Items.Add(new MojDefaultValueConfig { Value = value, TargetScenario = scenario });
        }

        public void Add(MojDefaultValueCommon value, string scenario = null)
        {
            CheckIs();
            Items.Add(new MojDefaultValueConfig { CommonValue = value, TargetScenario = scenario });
        }

        public void Add(MojDefaultValueAttr attr, string scenario = null)
        {
            CheckIs();
            Items.Add(new MojDefaultValueConfig { Attr = attr, TargetScenario = scenario });
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
            CArg(null, value, type);
        }

        [DataMember]
        public object Value { get; set; }
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
            Args = new List<MojAttrArg>();
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
        public List<MojAttrArg> Args { get; private set; }

        // KABU TODO: REMOVE?
        //[DataMember]
        //public Attribute Attr { get; set; }

        public MojAttr Clone()
        {
            var clone = (MojAttr)MemberwiseClone();
            clone.Args = new List<MojAttrArg>(Args.Select(x => x.Clone()));

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
            type = type ?? (value != null ? (Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType()) : null);

            if (!isConstructor && string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");

            // KABU TODO: REMOVE: We want to support null values.
            //if (string.IsNullOrWhiteSpace(valueStr))
            //    throw new ArgumentNullException("valueStr");

            Args.Add(new MojAttrArg { IsConstructorArg = isConstructor, Name = name, Value = value, ValueType = type, IsVerbatim = verbatim });

            return this;
        }

        public MojAttr ErrorArg(string errorMessage)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
                Args.Add(new MojAttrArg { Name = "ErrorMessage", Value = errorMessage, ValueType = typeof(string) });

            return this;
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