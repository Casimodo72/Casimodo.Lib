﻿using System.ComponentModel.DataAnnotations;

namespace Casimodo.Mojen
{
    // TODO: Think about: [StringLength(1)]
    public interface IMojPropBuilder
    {
        IMojPropBuilder Description(string description);

        IMojPropBuilder Remark(string remark);
    }

    public class MojPropBuilder
    {
        public static TPropBuilder Create<TPropBuilder>(MojTypeBuilder typeBuilder, MojProp prop, MojPropBuilder parentPropBuilder = null)
            where TPropBuilder : MojPropBuilder, new()
        {
            var builder = new TPropBuilder();
            builder.Initialize(typeBuilder, prop);
            builder.ParentPropBuilder = parentPropBuilder;
            return builder;
        }

        public void Initialize(MojTypeBuilder typeBuilder, MojProp prop)
        {
            App = typeBuilder.App;
            ObjectBuilder = typeBuilder;
            PropConfig = prop;
        }

        public MojPropBuilder ParentPropBuilder { get; set; }

        public MojenApp App { get; private set; }

        public MojProp PropConfig { get; set; }

        protected MojTypeBuilder ObjectBuilder { get; private set; }
    }

    public class MojPropBuilder<TObjectBuilder, TPropBuilder>
        : MojPropBuilder, IMojPropBuilder
        where TObjectBuilder : MojTypeBuilder<TObjectBuilder, TPropBuilder>
        where TPropBuilder : MojPropBuilder<TObjectBuilder, TPropBuilder>, new()
    {
        internal TObjectBuilder TypeBuilder
        {
            get { return (TObjectBuilder)base.ObjectBuilder; }
        }

        // KABU TODO: REMOVE
        //public virtual TPropBuilder Type(MojType type, bool nullable = false, bool nested = false, bool required = false, bool? navigation = true, bool? navigationOnModel = false)
        //{
        //    Guard.ArgNotNull(type, nameof(type));

        //    return TypeCore(type, nullable: nullable);
        //}

        public TPropBuilder Type(DataType type)
        {
            PropConfig.Type.AnnotationDataType = type;
            if (type == DataType.PhoneNumber)
            {
                // TODO: IMPORTANT: We can't use this regex because it is way to uintuitive.
                // DIN 5008: http://juergen-bayer.net/artikel/CSharp/Regulaere-Ausdruecke/Regulaere-Ausdruecke_002.aspx
                // RegEx(@"^\(\d{1,2}(\s\d{1,2}){1,2}\)\s(\d{1,2}(\s\d{1,2}){1,2})((-(\d{1,4})){0,1})$");
            }

            return This();
        }

        internal TPropBuilder TypeCore(MojType type, bool nullable = false)
        {
            if (type == null) throw new ArgumentNullException("type");

            PropConfig.Type.SetType(type, nullable);

            return This();
        }

        internal TPropBuilder Type(Type type)
        {
            PropConfig.Type.SetType(type);

            return This();
        }

        public TPropBuilder Description(IEnumerable<string> description)
        {
            if (description == null)
                return This();

            return Description(description.ToArray());
        }

        public TPropBuilder Description(params string[] description)
        {
            if (description == null || description.Length == 0)
                return This();

            foreach (var item in description)
                if (!PropConfig.Summary.Descriptions.Contains(item))
                    PropConfig.Summary.Descriptions.Add(item);

            return This();
        }

        public TPropBuilder Remark(string remark)
        {
            if (!PropConfig.Summary.Remarks.Contains(remark))
                PropConfig.Summary.Remarks.Add(remark);

            return This();
        }

        internal void UIHint(string hint)
        {
            Attr(new MojAttr("UIHint", 99).CSArg("uiHint", hint));
        }

        // KABU TODO: NOTUSED
        //internal void IgnoreDataMember()
        //{
        //    Attr(new MojAttr("IgnoreDataMember", 3));
        //}

        public TPropBuilder Attr(MojAttr attr)
        {
            if (!CanHaveAttr(attr))
                throw new MojenException($"The attribute '{attr.Name}' is not applicable to a property of this type.");

            PropConfig.Attrs.AddOrReplace(attr);

            return This();
        }

        public bool CanHaveAttr(MojAttr attr)
        {
            if ((attr.Name == "MaxLength" || attr.Name == "MinLength") &&
                !PropConfig.Type.IsArray)
            {
                return false;
            }

            if (attr.Name == "StringLength" && !PropConfig.Type.IsString)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Indicates that this property is implemented manually and will not be generated.
        /// </summary>        
        public TPropBuilder Custom()
        {
            PropConfig.IsCustom = true;

            return This();
        }

        // KABU TODO: REMOVE?
        //public TPropBuilder Map(bool map = true)
        //{
        //    PropConfig.IsMappedToStore = map;

        //    return This();
        //}

        public TPropBuilder Required()
        {
            return RequiredCore();
        }

        public TPropBuilder Required(string error = null)
        {
            return RequiredCore(error);
        }

        /// <summary>
        /// Entity: Generates a *non* null DB field constraint.
        /// </summary>
        TPropBuilder RequiredCore(string error = null)
        {
            // TODO: REMOVE
            //if (!PropConfig.IsKey && !PropConfig.Type.CanBeNull)
            //    throw new MojenException("Using 'Required' on a non-nullable property is invalid.");

            if (PropConfig.UseRules().IsNotRequired)
                throw new MojenException("Using 'Required' on an already *not* required property is invalid.");

            PropConfig.UseRules().IsRequired = true;

            // KABU TODO: VERY IMPORTANT: IMPL error message.

            if (PropConfig.IsForeignKey &&
                PropConfig.Reference.NavigationProp != null)
            {
                PropConfig.Reference.NavigationProp.UseRules().IsRequired = true;
            }

            return This();
        }

        public TPropBuilder NotRequired()
        {
            if (!PropConfig.IsKey && !PropConfig.Type.CanBeNull)
                throw new MojenException("Using 'NotRequired' on a non-nullable property is invalid.");

            if (PropConfig.UseRules().IsRequired)
                throw new MojenException("Using 'NotRequired' on an already required property is invalid.");

            PropConfig.UseRules().IsNotRequired = true;

            if (PropConfig.IsForeignKey &&
                PropConfig.Reference.NavigationProp != null)
            {
                PropConfig.Reference.NavigationProp.UseRules().IsNotRequired = true;
            }

            return This();
        }

        public TPropBuilder Decimal(bool nullable = true)
        {
            if (nullable)
                Type(typeof(decimal?));
            else
                Type(typeof(decimal));

            return This();
        }

        public TPropBuilder Money(bool nullable = false, bool includeNegative = false)
        {
            Decimal(nullable: nullable);
            if (!includeNegative)
            {
                Range(min: 0);
            }
            Precision(19, 4);
            Type(DataType.Currency);
            return This();
        }

        public TPropBuilder Percent(bool nullable = false)
        {
            Decimal(nullable: nullable);
            Precision(5, 2);
            Range(0, 100);
            return This();
        }

        public TPropBuilder VatPercent(bool nullable = false)
        {
            return Percent(nullable);
        }

        /// <summary>
        /// Specifies the decimal precision.
        /// </summary>
        /// <param name="precision">The number of all digits in a number.</param>
        /// <param name="scale">The number of digits to the right of the decimal point.</param>
        public TPropBuilder Precision(byte precision, byte scale, string error = null)
        {
            return Attr(new MojAttr("Precision", 4).CArg("precision", (int)precision).CArg("scale", (int)scale)
                .ErrorArg(error));
        }

        public TPropBuilder Date(bool nullable = true)
        {
            return DateTime(nullable: nullable, date: true, time: false);
        }

        public TPropBuilder Time(bool nullable = true, bool local = true)
        {
            return DateTime(nullable: nullable, date: false, time: true, local: local);
        }

        public TPropBuilder TimeSpan(bool nullable = true, bool days = false, bool hours = true, bool minutes = true)
        {
            // KABU TODO: IMPORTANT: TimeSpan produces problems on the JavaScript side.
            // We have to use custom controls and templates in order to edit and display TimeSpans in the browser.
            Type(nullable ? typeof(TimeSpan?) : typeof(TimeSpan));

            var info = PropConfig.Type.TimeSpanInfo;
            info.IsDays = days;
            info.IsHours = hours;
            info.IsMinutes = minutes;

            Type(DataType.Duration);

            return This();
        }

        public TPropBuilder DateOnly(bool nullable = true)
        {
            return DateTime(nullable, date: true, time: false, isDateOnly: true);
        }

        public TPropBuilder DateTime(bool nullable = true,
            bool date = true, bool time = true, bool isDateOnly = false,
            int ms = 0, bool local = true)
        {
            if (ms < 0 || ms > 3)
                throw new ArgumentOutOfRangeException("ms",
                    "The number of milliseconds must be greater than -2 and less than 4.");

            if (isDateOnly)
            {
                Type(nullable ? typeof(DateOnly?) : typeof(DateOnly));
            }
            else
            {
                Type(nullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset));
            }
            //if (!MojenUtils.IsDateTimeOrOffset(PropConfig.Type.TypeNormalized))
            //    throw new MojenException(string.Format("The property '{0}' must be of type DateTime or DateTimeOffset " +
            //        "in order to use the DateTime() builder method.", PropConfig.Name));

            var info = PropConfig.Type.DateTimeInfo;
            info.IsDate = date;
            info.IsDateOnly = isDateOnly;
            info.IsTime = time;
            info.IsLocal = time && local;
            info.DisplayMillisecondDigits = ms;

            if (info.IsDateAndTime)
            {
                Type(DataType.DateTime);
            }
            else
            {
                if (info.IsDate)
                    UIHint("Date");
                else
                    UIHint("Time");
            }

            if (nullable)
                DefaultValue(null);

            return This();
        }

        public TPropBuilder Range(int? min = null, int? max = null, string error = null)
        {
            RangeCore(min, max, typeof(int), int.MaxValue, error);

            PropConfig.UseRules().Min = min;
            PropConfig.UseRules().Max = max;

            return This();
        }

        public TPropBuilder Range(decimal? min = null, decimal? max = null, string error = null)
        {
            RangeCore(min, max, typeof(decimal), decimal.MaxValue, error);

            PropConfig.UseRules().Min = min;
            PropConfig.UseRules().Max = max;

            return This();
        }

        private void RangeCore(object min, object max, Type type, object maxValue, string error)
        {
            if (min == null && max == null)
                throw new MojenException("At least one of min or max or both must be specified.");

            var attr = new MojAttr("Range", 4);

            if (min != null)
                attr.CArg("minimum", min, type);

            attr.CArg("maximum", max != null ? max : maxValue, type);

            attr.ErrorArg(error);

            Attr(attr);
        }

        public TPropBuilder MinLength(int length, string error = null)
        {
            return MinMaxLengthCore("MinLength", length, error);
        }

        public TPropBuilder MaxLength(int length, string error = null)
        {
            return MinMaxLengthCore("MaxLength", length, error);
        }

        public TPropBuilder MinMaxLengthCore(string name, int length, string error = null)
        {
            if (name == "MaxLength")
                PropConfig.UseRules().Max = length;
            else
                PropConfig.UseRules().Min = length;

            if (PropConfig.Type.IsString)
            {
                var attr = PropConfig.Attrs.FindOrCreate("StringLength", 5);
                if (name == "MinLength")
                    attr.PArg("MinimumLength", length);
                else
                    attr.CArg("maximumLength", length);

                return Attr(attr);
            }
            else
            {
                if (PropConfig.Attrs.Contains(name))
                    throw new Exception($"{name} was already applied.");

                return Attr(new MojAttr(name, 5).CArg("length", length).ErrorArg(error));
            }
        }

        public TPropBuilder RegEx(string pattern, string error = null)
        {
            return Attr(new MojAttr("RegularExpression", 6).CSArg("pattern", pattern, verbatim: true).ErrorArg(error));
            // RegularExpression(expression) { ErrorMessage = error };
        }

        public TPropBuilder DefaultValueOnEdit(MojDefaultValueCommon value)
        {
            return DefaultValueCore(value, "OnEdit");
        }

        TPropBuilder DefaultValueCore(MojDefaultValueCommon value, string scenario)
        {
            PropConfig.AddDefaultValue(value, scenario);
            return This();
        }

        public TPropBuilder DefaultValueOnEdit(object value)
        {
            PropConfig.AddDefaultValue(value, "OnEdit");
            return This();
        }

        public TPropBuilder DefaultTime(int hours, int minutes)
        {
            return DefaultValue(new DateTimeOffset(2015, 1, 1, hours, minutes, 0, System.TimeSpan.Zero));
        }

        public TPropBuilder DefaultValue(object value)
        {
            return DefaultValueCore(value, null);
        }

        TPropBuilder DefaultValueCore(object value, string scenario)
        {
            value = PropConfig.Type.ValidateAndConvertValue(value);

            var attr = new MojDefaultValueAttr(value, PropConfig.Type.TypeNormalized);
            if (!CanHaveAttr(attr))
                throw new MojenException($"The attribute '{attr.Name}' is not applicable to a property of this type.");

            PropConfig.AddDefaultValue(attr, scenario: scenario);
            return This();
        }

        public TPropBuilder Display(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return This();

            PropConfig.DisplayLabel = name;

            return Attr(new MojAttr("Display", 20).PSArg("Name", name));
            // new DisplayAttribute { Name = name };
        }

        public TPropBuilder UseColor()
        {
            PropConfig.UseColor = true;

            return This();
        }

        internal TPropBuilder ForeignKeyAttr(string keyPropName)
        {
            if (string.IsNullOrWhiteSpace(keyPropName)) throw new ArgumentNullException(nameof(keyPropName));

            // [ForeignKey(name: "KeyPropName")];
            return Attr(new MojAttr("ForeignKey", 30).CSArg("name", keyPropName));
        }

        public TPropBuilder Color(bool opacity = false)
        {
            PropConfig.IsColor = true;
            PropConfig.IsColorWithOpacity = opacity;
            PropConfig.IsSortable = false;
            PropConfig.IsFilterable = false;
            PropConfig.IsGroupable = false;

            return This();
        }

        public TPropBuilder VerMapFrom(string name = null, string value = null)
        {
            var map = EnsureVerMap();
            map.HasSource = true;
            map.SourceName = name ?? PropConfig.Name;
            if (value != null)
                map.ValueExpression = value;
            return This();
        }

        public TPropBuilder VerMapTo(string value)
        {
            var map = EnsureVerMap();
            map.HasSource = false;
            map.ValueExpression = value;
            return This();
        }

        public TPropBuilder NoVerMap()
        {
            PropConfig.VerMap = MojVersionMapping.None;
            PropConfig.StoreOrSelf.VerMap = MojVersionMapping.None;
            return This();
        }

        MojVersionMapping EnsureVerMap()
        {
            if (!PropConfig.VerMap.Is) PropConfig.VerMap = new MojVersionMapping();
            return PropConfig.VerMap;
        }

        public TPropBuilder DataMember()
        {
            if (TypeBuilder.TypeConfig.NoDataContract)
                throw new MojenException(string.Format("The property '{0}' cannot be a [DataMember] " +
                    "because its type '{1}' is not a [DataContract].",
                    PropConfig.Name, TypeBuilder.TypeConfig.Name));

            return Attr(new MojAttr("DataMember", 1));
        }

        public MojProp GetProp()
        {
            return PropConfig;
        }

        protected TPropBuilder This()
        {
            return (TPropBuilder)this;
        }

        IMojPropBuilder IMojPropBuilder.Description(string description)
        {
            return Description(description);
        }

        IMojPropBuilder IMojPropBuilder.Remark(string remark)
        {
            return Remark(remark);
        }
    }
}