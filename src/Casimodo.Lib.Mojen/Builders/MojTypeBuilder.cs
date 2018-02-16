using System;
using System.ComponentModel.DataAnnotations;

namespace Casimodo.Lib.Mojen
{
    public interface IMojTypeBuilder
    {
        IMojPropBuilder Prop(MojProp prop);
    }

    public class MojTypeBuilder : IMojTypeBuilder
    {
        public static TTypeBuilder Create<TTypeBuilder>(MojenApp app, MojType type)
            where TTypeBuilder : MojTypeBuilder, new()
        {
            var builder = new TTypeBuilder();
            builder.Initialize(app, type);
            return builder;
        }

        public void Initialize(MojenApp app, MojType type)
        {
            App = app;
            TypeConfig = type;
        }

        public MojenApp App { get; private set; }

        public MojType TypeConfig { get; internal set; }

        /// <summary>
        /// NOTE: This clones the given property. I.e. the given property will not be modified.
        /// </summary>
        public MojPropBuilder Prop(MojProp prop)
        {
            if (prop == null) throw new ArgumentNullException("prop");

            // Clone
            prop = prop.Clone();
            TypeConfig.AddLocalProp(prop);

            var _pbuilder = MojPropBuilder.Create<MojPropBuilder>(this, prop);

            // Clone store prop in applicable.
            if (TypeConfig.Store != null && prop.Store != null)
            {
                var storeProp = prop.Store.Clone();
                TypeConfig.Store.AddLocalProp(storeProp);

                prop.Store = storeProp;
            }

            return _pbuilder;
        }

        IMojPropBuilder IMojTypeBuilder.Prop(MojProp prop)
        {
            return (IMojPropBuilder)Prop(prop);
        }
    }

    public abstract class MojTypeBuilder<TTypeBuilder, TPropBuilder> : MojTypeBuilder, IMojTypeBuilder
        where TTypeBuilder : MojTypeBuilder<TTypeBuilder, TPropBuilder>
        where TPropBuilder : MojPropBuilder<TTypeBuilder, TPropBuilder>, new()
    {
        protected TPropBuilder _pbuilder;

        public TTypeBuilder Name(string name)
        {
            TypeConfig.InitName(name);

            return This();
        }

        public TTypeBuilder Id(string guid)
        {
            TypeConfig.Id = new Guid(guid);

            return This();
        }

        public TTypeBuilder Display(string name, string plural = null)
        {
            TypeConfig.DisplayName = name;
            TypeConfig.DisplayPluralName = plural ?? name;

            return This();
        }

        public TTypeBuilder Dir(string folder)
        {
            TypeConfig.OutputDirPath = folder;

            return This();
        }

        public TTypeBuilder Namespace(string ns)
        {
            TypeConfig.Namespace = ns;

            return This();
        }

        public TTypeBuilder NoDataContract()
        {
            TypeConfig.NoDataContract = true;

            return This();
        }

        public TTypeBuilder AuthorizedRoles(string roles)
        {
            TypeConfig.AuthRoles = roles;

            return This();
        }

        public TTypeBuilder Tenant()
        {
            TypeConfig.IsTenant = true;

            App.CurrentBuildContext.Get<DataLayerConfig>().Tenant = TypeConfig;

            return This();
        }

        public TTypeBuilder VerMap(string name = null, bool implicitProps = true, string[] ignoreSource = null)
        {
            var map = EnsureVerMap();
            map.HasSource = true;
            if (name != null)
            {
                map.SourceName = name;
                map.SourcePluralName = MojType.Pluralize(map.SourceName);
            }
            else
            {
                map.SourceName = TypeConfig.Name;
                map.SourcePluralName = TypeConfig.PluralName;
            }
            if (ignoreSource != null && ignoreSource.Length != 0)
                map.IgnoreSourceProps.AddRange(ignoreSource);

            map.IsIncludePropsByDefault = implicitProps;

            return This();
        }

        /// <summary>
        /// Ignores a property of the source type being mapped from.
        /// </summary>
        public TTypeBuilder VerMapFromIgnore(string name)
        {
            if (!TypeConfig.VerMap.Is) throw new MojenException("The type does not define a previous version.");

            TypeConfig.VerMap.IgnoreSourceProps.Add(name);

            return This();
        }

        /// <summary>
        /// Maps an inherited property using the given setter expression.
        /// </summary>
        public TTypeBuilder VerMapTo(string name, bool? source, string value)
        {
            var map = TypeConfig.VerMap;
            if (!map.Is) throw new MojenException("The type does not define a previous version.");

            map.ToPropOverrides.Add(new MojVersionMapping
            {
                HasSource = source,
                TargetName = name,
                ValueExpression = value ?? "null"
            });

            return This();
        }

        MojVersionMapping EnsureVerMap()
        {
            if (!TypeConfig.VerMap.Is) TypeConfig.VerMap = new MojVersionMapping();
            return TypeConfig.VerMap;
        }

        /// <summary>
        /// Indicates that this type was implemented manually and will not be generated.
        /// </summary>
        /// <returns></returns>
        public TTypeBuilder Custom()
        {
            TypeConfig.ExistsAlready = true;
            return This();
        }

        protected internal TPropBuilder Prop(string name, int max)
        {
            return Prop(name, typeof(string))
                .MaxLength(max);
        }

        protected virtual void OnPropAdding(MojProp prop)
        {
            // NOP
        }

        public virtual TPropBuilder Prop(string name, Type type = null, MojType mojtype = null, bool nullableMojType = false, bool related = false)
        {
            if (type != null && mojtype != null)
                throw new ArgumentException("The arguments type and modelType are mutually exclusive.");

            // Create new property.
            var prop = new MojProp();
            prop.DeclaringType = TypeConfig;
            prop.Initialize(name);
            // Inherit IsObservablue from containing type.
            prop.IsObservable = TypeConfig.IsObservable;
            prop.IsAutoRelated = related;

            OnPropAdding(prop);

            // Add new property.
            TypeConfig.LocalProps.Add(prop);

            _pbuilder = MojPropBuilder.Create<TPropBuilder>(this, prop);

            if (type == null && mojtype == null)
                type = typeof(string);

            if (mojtype != null)
            {
                var effectiveMojType = mojtype;
                if (mojtype.Kind == MojTypeKind.Model &&
                    TypeConfig.Kind == MojTypeKind.Entity)
                {
                    effectiveMojType = mojtype.Store;
                    if (effectiveMojType == null)
                        throw new MojenException(string.Format("Cannot use model type '{0}' as property type of entity '{1}' " +
                            "because the model type has no underlying store entity type.",
                            mojtype.Name, TypeConfig.Name));
                }
                _pbuilder.TypeCore(effectiveMojType, nullable: nullableMojType);
            }
            else
            {
                _pbuilder.Type(type);

                // Booleans have an implicit default value of FALSE.
                if (type == typeof(bool))
                {
                    _pbuilder.DefaultValue(false);
                }
            }

            if (!TypeConfig.HasBaseClass)
                _pbuilder.PropConfig.IsObservable = false;

            // Implicitely inherit version mapping from declaring type.
            if (TypeConfig.VerMap.Is && TypeConfig.VerMap.IsIncludePropsByDefault)
                _pbuilder.PropConfig.VerMap = new MojVersionMapping { HasSource = true };

            return _pbuilder;
        }

        public TTypeBuilder Description(string description)
        {
            TypeConfig.Summary.Descriptions.Add(description);

            return This();
        }

        public TTypeBuilder Remark(string remark)
        {
            TypeConfig.Summary.Remarks.Add(remark);

            return This();
        }

        public TTypeBuilder Attr(MojAttr attr)
        {
            TypeConfig.Attrs.Add(attr);

            return This();
        }

        public TTypeBuilder Use<T>(dynamic parameters = null)
            where T : MojenGenerator
        {
            TypeConfig.UsingGenerators.Add(new MojUsingGeneratorConfig { Type = typeof(T), Args = parameters });
            return This(); ;
        }

        public virtual MojType Build()
        {
            return TypeConfig;
        }

        protected TTypeBuilder This()
        {
            return (TTypeBuilder)this;
        }
    }
}