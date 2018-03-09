using Casimodo.Lib.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    public enum MojMultilineStringMode
    {
        Default = 0,
        /// <summary>
        /// Linebreak separated list of values.
        /// </summary>
        List = 1
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojMultilineStringType : MojBase
    {
        public static readonly MojMultilineStringType None = new MojMultilineStringType { Is = false };

        public MojMultilineStringType()
        {
            Is = true;
        }

        [DataMember]
        public bool Is { get; private set; }

        [DataMember]
        public MojMultilineStringMode Mode { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojPropType : MojBase
    {
        public static MojPropType Create(Type type)
        {
            var item = new MojPropType();
            item.SetType(type);
            return item;
        }

        public static MojPropType Create(MojType type, bool nullable)
        {
            var item = new MojPropType();
            item.SetType(type, nullable);

            return item;
        }

        public MojPropType()
        {
            // KABU TODO: REMOVE
            //DateTimeInfo = new MojDateTimeInfo();
        }

        public MojPropType Clone()
        {
            var clone = (MojPropType)MemberwiseClone();

            return clone;
        }

        public MojPropType ConvertToEntity()
        {
            if (TypeConfig.IsModel())
                SetType(TypeConfig.RequiredStore, IsNullableValueType);

            if (HasGenericTypeArguments && GenericTypeArguments.Any(x => x.TypeConfig.IsModel()))
            {
                var prevArgs = _genericTypeArguments;
                _genericTypeArguments = new List<MojPropType>();
                foreach (var t in prevArgs)
                {
                    if (t.TypeConfig.IsModel())
                        _genericTypeArguments.Add(t.Clone().ConvertToEntity());
                    else
                        _genericTypeArguments.Add(t);
                }
            }

            BuildName(TypeConfig);

            return this;
        }

        /// <summary>
        /// Only used at data layer to check for valid MojType kind.
        /// </summary>
        public MojProp DeclaringProp { get; internal set; }

        /// <summary>
        /// Only used in data layer for the build of Name and NameNormalized.
        /// </summary>
        public MojType CustomType { get; set; }

        /// <summary>
        /// (If the type is a *nullable* value type then this returns MyType?)
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Returns the type's name or the underlying type's name in case of nullable value types.
        /// </summary>
        [DataMember]
        public string NameNormalized { get; set; }

        /// <summary>
        /// NOTE: Will be NULL if the type is an enum.
        /// </summary>
        public Type Type { get; set; }

        [DataMember]
        string _typeFullName { get; set; }

        /// <summary>
        /// NOTE: Will be int if the type is an enum.
        /// </summary>
        public Type TypeNormalized { get; set; }

        [DataMember]
        string _typeNormalizedFullName { get; set; }

        public bool IsModel()
        {
            return (TypeConfig.IsModel() || GenericTypeArguments.Any(x => x.TypeConfig.IsModel()));
        }

        /// <summary>
        /// Not serialized.
        /// </summary>
        public MojType TypeConfig { get; set; }

        /// <summary>
        /// Indicates that the property is a reference to an other object.
        /// </summary>
        [DataMember]
        public bool IsReference { get; set; }

        public bool IsString
        {
            get { return !IsReference && Type == typeof(string); }
        }

        public bool IsMultilineString
        {
            get { return IsString && MultilineString.Is; }
        }

        public bool IsInteger
        {
            get { return !IsReference && Type.IsInteger(); }
        }

        public bool IsDecimal
        {
            get { return !IsReference && Type.IsDecimal(); }
        }

        public bool IsNumber
        {
            get { return !IsReference && Type.IsNumber(); }
        }

        public bool IsBoolean
        {
            get { return TypeNormalized == typeof(bool); }
        }

        public bool IsPrimitiveArray
        {
            get { return IsArray && TypeNormalized.GetElementType().IsPrimitive; }
        }

        public bool IsByteArray
        {
            get { return IsArray && TypeNormalized.GetElementType() == typeof(byte); }
        }

        public bool IsArray
        {
            get { return TypeNormalized != null && TypeNormalized.IsArray; }
        }

        /// <summary>
        /// Returns true for DateTimeOffset or DateTime.
        /// </summary>
        public bool IsAnyTime
        {
            get { return MojenUtils.IsDateTimeOrOffset(TypeNormalized); }
        }

        public bool IsTimeSpan
        {
            get { return TypeNormalized != null && TypeNormalized == typeof(TimeSpan); }
        }

        /// <summary>
        /// Indicates whether the type of the property is an enum.
        /// </summary>
        [DataMember]
        public bool IsEnum { get; set; }

        [DataMember]
        public bool IsNullableValueType { get; set; }

        public bool CanBeNull
        {
            get
            {
                return IsNullableValueType ||
                    // ForeignKey != null ||
                    (Type != null && !Type.IsPrimitive && !Type.IsValueType) ||
                    (!IsPrimitive && !IsValueType);
            }
        }

        [DataMember]
        public bool IsPrimitive { get; set; }

        [DataMember]
        public bool IsValueType { get; set; }

        [DataMember]
        public MojDateTimeInfo DateTimeInfo { get; private set; }

        [DataMember]
        public MojTimeSpanInfo TimeSpanInfo { get; private set; }

        [DataMember]
        /// <summary>
        /// Indicates whether this type is a collection.
        /// </summary>
        public bool IsCollection { get; set; }

        [DataMember]
        public string CollectionTypeName { get; set; }

        public bool IsDictionary
        {
            get { return TypeNormalized != null && TypeNormalized.GetInterface("IDictionary") != null; }
        }

        public bool HasGenericTypeArguments
        {
            get { return _genericTypeArguments != null && _genericTypeArguments.Count != 0; }
        }

        public List<MojPropType> GenericTypeArguments
        {
            get { return _genericTypeArguments ?? (_genericTypeArguments = new List<MojPropType>()); }
        }

        [DataMember]
        List<MojPropType> _genericTypeArguments;

        [DataMember]
        public DataType? AnnotationDataType { get; set; }

        [DataMember]
        public MojMultilineStringType MultilineString { get; set; } = MojMultilineStringType.None;

        public object ValidateAndConvertValue(object value)
        {
            if (value == null && !CanBeNull)
                throw new MojenException("Null is not a valid value for this property type.");

            var valueType = value?.GetType();
            if (valueType != null &&
                valueType != Type &&
                valueType != TypeNormalized)
            {
                // Allow strings for GUIDs.
                if (TypeNormalized == typeof(Guid) && valueType == typeof(string))
                {
                    Guid g;
                    if (Guid.TryParse((string)value, out g))
                        return value;
                }

                // Allow integers for numbers and convert.
                if (IsNumber && valueType == typeof(int))
                {
                    return Convert.ChangeType(value, Type);
                }

                throw new MojenException($"The value '{value}' is of different type than the property's type '{Name}'.");
            }

            return value;
        }

        public void SetType(MojType type, bool nullable = false)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (DeclaringProp != null &&
                DeclaringProp.DeclaringType.Kind == MojTypeKind.Entity &&
                type.Kind == MojTypeKind.Model)
            {
                throw new MojenException("An entity property cannot have a model type as its property type.");
            }

            IsEnum = type.Kind == MojTypeKind.Enum;
            if (IsEnum)
            {
                IsPrimitive = false;
                IsValueType = true;
                IsNullableValueType = nullable;

                Type = (nullable ? typeof(int?) : typeof(int));
                TypeNormalized = typeof(int);
            }
            else
            {
                if (nullable)
                    throw new MojenException($"Properties of type '{nameof(MojType)}' can only be nullable when the type defines an enum.");

                TypeConfig = type;

                Type = null;
                TypeNormalized = null;

                IsPrimitive = false;
                IsValueType = false;
                IsNullableValueType = false;
            }

            BuildName(type);
        }

        public void SetType(Type type)
        {
            Guard.ArgNotNull(type, nameof(type));

            if (type == typeof(string) && AnnotationDataType == null)
                AnnotationDataType = DataType.Text;

            if (type != typeof(string) && AnnotationDataType != null)
                AnnotationDataType = null;

            Type normalizedType = type;
            bool nullable = false;
            if (type.IsValueType)
            {
                if (TypeHelper.IsNullableType(type))
                {
                    nullable = true;
                    normalizedType = Nullable.GetUnderlyingType(type);
                }
            }

            Type = type;
            TypeNormalized = normalizedType;
            IsValueType = normalizedType.IsValueType;
            IsNullableValueType = nullable;
            IsPrimitive = normalizedType.IsPrimitive;
            var isDictionary = normalizedType.GetInterface("IDictionary") != null;
            IsCollection = !isDictionary && normalizedType.GetInterface("ICollection") != null;

            Name = MojenUtils.ToCsType(normalizedType, nullable);
            NameNormalized = MojenUtils.ToCsType(normalizedType);

            if (normalizedType.IsGenericType)
            {
                // Add generic type arguments.
                foreach (var argType in normalizedType.GetGenericArguments())
                    GenericTypeArguments.Add(MojPropType.Create(argType));
            }

            if (MojenUtils.IsDateTimeOrOffset(normalizedType))
            {
                DateTimeInfo = new MojDateTimeInfo();
                DateTimeInfo.IsDate = true;
                DateTimeInfo.IsTime = true;
            }

            if (normalizedType == typeof(TimeSpan))
            {
                TimeSpanInfo = new MojTimeSpanInfo();
                TimeSpanInfo.IsHours = true;
                TimeSpanInfo.IsMinutes = true;
            }
        }

        public void SetEnum(string type, bool nullable)
        {
            Name = type + (nullable ? "?" : "");
            Type = (nullable ? typeof(int?) : typeof(int));
            TypeNormalized = typeof(int);
            NameNormalized = type;
            IsEnum = true;
            IsNullableValueType = nullable;
            IsValueType = true;
            IsPrimitive = false;
            IsCollection = false;
        }

        public void BuildName(MojType type)
        {
            if (IsEnum)
            {
                if (type != null)
                {
                    Name = type.ClassName + (IsNullableValueType ? "?" : "");
                    NameNormalized = type.ClassName;
                }
            }
            else if (IsCollection && CollectionTypeName != null)
            {
                Name = $"{CollectionTypeName}<{GenericTypeArguments[0].Name}>";
                NameNormalized = Name;
            }
            else
            {
                if (type != null)
                {
                    Name = type.ClassName;
                    NameNormalized = type.ClassName;
                }
            }
        }

        /// <summary>
        /// NOTE: Use only if no other option available.
        /// </summary>        
        public void SetCustom(string type)
        {
            Type = null;
            TypeNormalized = null;
            Name = type;
            NameNormalized = type;
            IsEnum = false;
            IsValueType = false;
            IsNullableValueType = false;
            IsPrimitive = false;
            IsCollection = false;
        }

        public override string ToString()
        {
            return Name;
        }

        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            _typeNormalizedFullName = TypeNormalized != null ? TypeNormalized.FullName : null;
            _typeFullName = Type != null ? Type.FullName : null;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (_typeNormalizedFullName != null)
                TypeNormalized = Type.GetType(_typeNormalizedFullName);
            if (_typeFullName != null)
                Type = Type.GetType(_typeFullName);
        }
    }
}