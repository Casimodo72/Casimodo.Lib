using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Casimodo.Lib.Mojen
{
    public class MojViewCollectionPropBuilder : MojViewPropBuilderBase<MojViewCollectionPropBuilder>
    {
        public static MojViewCollectionPropBuilder Create(MojViewBuilder itemViewBuilder, MojFormedType type, MojProp prop)
        {
            var builder = new MojViewCollectionPropBuilder();
            builder.Initialize(itemViewBuilder, null, prop);
            builder.FormedType = type;

            return builder;
        }

        MojFormedType FormedType { get; set; }

        public MojViewCollectionPropBuilder TagSelector(string viewId)
        {
            Prop.IsSelector = true;
            Prop.IsTagsSelector = true;

            Prop.Lookup = new MojLookupViewPropConfig
            {
                Is = true,
                TargetType = Prop.Reference.ToType,
                ViewId = viewId
            };

            return This();
        }

        public void Content(Action<MojViewBuilder> build)
        {
            var contentView = new MojViewConfig();
            contentView.RootView = View.RootView;
            contentView.TypeConfig = View.TypeConfig;
            var contentViewBuilder = new MojViewBuilder(contentView);
            contentView.Template.ViewBuilder = contentViewBuilder;

            build(contentViewBuilder);

            Prop.ContentView = contentViewBuilder.Build();
        }
    }

    public abstract class MojViewPropBuilderBase<TBuilder>
        where TBuilder : MojViewPropBuilderBase<TBuilder>
    {
        static readonly List<string> CloneFromModelExcludedProps = new List<string> { };

        protected MojViewBuilder _viewBuilder;

        protected virtual void Initialize(MojViewBuilder viewBuilder, MojViewProp viewProp, MojProp sourceProp)
        {
            _viewBuilder = viewBuilder;
            if (viewProp != null)
                Prop = viewProp;
            else
            {
                Prop = new MojViewProp(sourceProp);
                Type vt = typeof(MojViewProp);
                PropertyInfo vp;
                FieldInfo vf;

                var flags = BindingFlags.Instance | BindingFlags.Public;

                // Assign property values from MojProp to MojViewProp.
                foreach (var p in sourceProp.GetType().GetProperties(flags))
                {
                    if (CloneFromModelExcludedProps.Contains(p.Name))
                        continue;

                    if (!p.CanWrite)
                        continue;

                    vp = vt.GetProperty(p.Name, flags);

                    vp.SetValue(Prop, p.GetValue(sourceProp));
                }

                // Fields
                foreach (var f in sourceProp.GetType().GetFields(flags))
                {
                    vf = vt.GetField(f.Name, flags);
                    vf.SetValue(Prop, f.GetValue(sourceProp));
                }

                // NOTE: Some properties must be reset.
                Prop.InitialSort = MojOrderConfig.None;
            }
        }

        public MojViewProp Prop { get; private set; }

        protected TBuilder This()
        {
            return (TBuilder)this;
        }

        protected MojType TypeConfig
        {
            get { return _viewBuilder.View.TypeConfig; }
        }

        protected MojViewConfig View
        {
            get { return _viewBuilder.View; }
        }

        protected MojViewBuilder ViewBuilder
        {
            get { return _viewBuilder; }
        }

        public TBuilder Label(string label)
        {
            Prop.DisplayLabel = label;

            return This();
        }

        public TBuilder UseCustomTemplate(string name)
        {
            Prop.CustomTemplateName = name;

            return This();
        }

        public TBuilder ShowIf(MojProp prop)
        {
            // KABU TODO: IMPL?

            return This();
        }

        public TBuilder ShowIf(MojFormedType type)
        {
            // KABU TODO: IMPL?

            return This();
        }

        public TBuilder HideOn(MojViewMode mode)
        {
            Prop.HideModes = mode;
            return This();
        }

        public TBuilder ShowOn(MojViewMode mode)
        {
            //Prop.ShowModes = mode;
            Prop.HideModes = MojViewMode.All & ~mode;
            return This();
        }
    }

    public class MojViewPropBuilder : MojViewPropBuilderBase<MojViewPropBuilder>
    {
        public static MojViewPropBuilder Create(MojViewBuilder view, MojProp prop)
        {
            var builder = new MojViewPropBuilder();
            builder.Initialize(view, null, prop);

            return builder;
        }

        public static MojViewPropBuilder Create(MojViewBuilder view, MojViewProp vprop)
        {
            var builder = new MojViewPropBuilder();
            builder.Initialize(view, vprop, null);

            return builder;
        }

        public MojViewPropBuilder DefaultValue(object value)
        {
            Prop.AddDefaultValue(value, "OnEdit");
            return this;
        }

        /// <summary>
        /// Display only the time portion.
        /// </summary>
        public MojViewPropBuilder Time()
        {
            Prop.DisplayDateTime = Prop.Type.DateTimeInfo.Clone();
            Prop.DisplayDateTime.IsDate = false;

            return this;
        }

        public MojViewPropBuilder Link()
        {
            // KABU TODO: IMPL

            return this;
        }

        public MojViewPropBuilder Rows(int value)
        {
            if (Prop.Type.AnnotationDataType != DataType.MultilineText)
                throw new MojenException("The directive 'Row' can only be used on multiline text properties.");

            Prop.RowCount = value;

            return this;
        }       

        public MojViewPropBuilder Sortable(bool sortable = true)
        {
            Prop.IsSortable = sortable;

            return this;
        }

        public MojViewPropBuilder Width(int width)
        {
            Prop.Width = width;

            return this;
        }

        public MojViewPropBuilder IsHtml()
        {
            if (!Prop.Type.IsString)
                throw new MojenException($"'{nameof(IsHtml)}' is applicable to string properties only.");

            Prop.IsHtml = true;
            return this;
        }

        public MojViewPropBuilder AsHtml()
        {
            if (!Prop.Type.IsString)
                throw new MojenException($"'{nameof(AsHtml)}' is applicable to string properties only.");

            Prop.IsDisplayedAsHtml = true;
            return this;
        }

        public MojViewPropBuilder Color(MojProp colorProp)
        {
            if (!TypeConfig.IsAccessibleFromThis(colorProp))
                throw new MojenException($"Property '{colorProp.Name}' cannot be accessed from type '{TypeConfig.ClassName}'.");

            if (!colorProp.IsColor)
                throw new MojenException($"Property '{colorProp.Name}' is not a color property.");

            Prop.ColorProp = colorProp;

            return this;
        }

        public MojViewPropBuilder Distinct()
        {
            if (!Prop.FormedNavigationTo.Is)
                throw new MojenException("The directive 'Distinct' is only applicable to references.");

            Prop.IsReferenceLookupDistinct = true;

            return this;
        }

        public MojViewPropBuilder Filter(Action<MexConditionBuilder> build)
        {
            Prop.Predicate = BuildPredicate(build);

            return this;
        }

        MexExpressionNode BuildPredicate(Action<MexConditionBuilder> build)
        {
            if (build == null)
                return null;

            var predicateBuilder = new MexConditionBuilder();
            build(predicateBuilder);
            return predicateBuilder.Expression;
        }

        public MojViewPropBuilder InitialSort(MojOrderDirection direction = MojOrderDirection.Ascending, int index = 1)
        {
            Prop.InitialSort = new MojOrderConfig
            {
                Name = Prop.Name,
                Index = (index <= 0 ? 0 : index),
                Direction = direction
            };

            return this;
        }

        public MojViewPropBuilder Filterable(bool value = true)
        {
            Prop.IsFilterable = value;

            return this;
        }

        public MojViewPropBuilder ReadOnly()
        {
            return Editable(false);
        }

        public MojViewPropBuilder Strong()
        {
            Prop.FontWeight = MojFontWeight.Bold;

            return this;
        }

        public MojViewPropBuilder Snippets(int type)
        {
            if (!Prop.Type.IsString)
                throw new MojenException($"Property '{Prop.Name}': Cannot defined snippet editor for a non-string property.");

            Prop.Snippets = new MojPropSnippetsConfig
            {
                Is = true
            };
            // KABU TODO: MAGIC: "TypeId"
            Prop.Snippets.Args.Add("TypeId", type);

            return this;
        }

        public MojViewPropBuilder Editable(bool value = true)
        {
            Prop.IsEditable = value;

            return this;
        }

        public MojViewPropBuilder AutoCompleteFilter(int position = 0)
        {
            Prop.AutoCompleteFilter.IsEnabled = true;
            Prop.AutoCompleteFilter.PropName = Prop.Name;
            Prop.AutoCompleteFilter.Position = position;

            return this;
        }

        public MojViewPropBuilder CascadeFrom(MojFormedType type)
        {
            if (!Prop.IsSelector)
                throw new MojenException($"Cascade from is allowed for selectors only.");

            if (!TypeConfig.IsAccessibleFromThis(type))
                throw new MojenException($"Type '{type._Type.ClassName}' cannot be accessed from type '{TypeConfig.ClassName}'.");

            if (type.FormedNavigationFrom.Steps.Count > 1)
                throw new MojenException($"Cascade from is allowed for direct properties only.");

            Prop.CascadeFrom = type;
            return this;
        }

        public MojViewPropBuilder CascadeFromScope(MojProp source, MojProp target)
        {
            if (!Prop.Lookup.Is || Prop.CascadeFrom == null)
                throw new MojenException("CascadeFromScope is only applicable for Lookup view properties with CascadeFrom.");

            var targetType = Prop.FormedNavigationTo.TargetType;

            if (!targetType.IsAccessibleFromThis(target))
                throw new MojenException($"Property '{target.FormedTargetPath}' cannot be navigated to from lookup target type '{targetType.ClassName}'.");

            Prop.CascadeFromScope = new MojLookupCascadeFromScopeViewPropConfig
            {
                SourceProp = source,
                TargetProp = target
            };

            return this;
        }

        /// <summary>
        /// KABU TODO: The property with the lookup definition actually won't be displayed,
        /// so IMPL definition of lookups without having to specify a property.
        /// </summary>
        /// <returns></returns>
        public MojViewPropBuilder Lookup(string group = null, string viewId = null)
        {
            Prop.IsSelector = true;
            Prop.Lookup = new MojLookupViewPropConfig
            {
                Is = true,
                TargetType = Prop.FormedNavigationTo.TargetType,
                ViewGroup = group,
                ViewId = viewId
            };

            return this;
        }

        public MojViewPropBuilder Selector()
        {
            Prop.IsSelector = true;

            return this;
        }

        public MojViewPropBuilder GeoPlaceLookup(GeoPlaceSourcePropMap sourcePropMap = null, bool cacheView = false)
        {
            Prop.IsSelector = true;
            Prop.GeoPlaceLookup = new GeoPlaceLookupConfig
            {
                IsViewCached = cacheView,
                SourcePropMap = sourcePropMap
            };

            return this;
        }

        public MojViewPropBuilder Header()
        {
            Prop.IsHeader = true;

            return this;
        }

        public MojViewPropBuilder FilterText()
        {
            Prop.IsDataFilterText = true;

            return this;
        }

        /// <summary>
        /// Applies server side ordering regardless of the client's request.
        /// </summary>
        public MojViewPropBuilder OrderBy()
        {
            Prop.OrderByIndex = 1;

            return this;
        }
    }
}