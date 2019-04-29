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
            builder.Initialize(itemViewBuilder, prop);
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

        protected MojViewBuilder ViewBuilder { get; private set; }

        protected virtual void Initialize(MojViewBuilder viewBuilder, MojProp sourceProp)
        {
            ViewBuilder = viewBuilder;

            Prop = new MojViewProp(viewBuilder.View, sourceProp);
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

        public MojViewProp Prop { get; private set; }

        protected TBuilder This()
        {
            return (TBuilder)this;
        }

        protected MojType TypeConfig
        {
            get { return ViewBuilder.View.TypeConfig; }
        }

        public MojViewConfig View
        {
            get { return ViewBuilder.View; }
        }

        public TBuilder Label(string label)
        {
            Prop.DisplayLabel = label;

            return This();
        }

        public TBuilder UseTemplate(string name, string propPath = null)
        {
            Prop.CustomTemplateName = name;
            Prop.CustomTemplatePropPath = propPath;

            return This();
        }

        public TBuilder Substring(int startIndex, int? length = null)
        {
            Prop.StringSubstring = new MojStringSubstringConfig { Is = true };
            Prop.StringSubstring.StartIndex = startIndex;
            Prop.StringSubstring.Length = length;

            return This();
        }

        public TBuilder CustomEditorView(string name)
        {
            Prop.CustomEditorViewName = name;

            return This();
        }

        public TBuilder CustomView(string name)
        {
            Prop.CustomViewName = name;

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
            builder.Initialize(view, prop);

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
            Prop.IsLinkToInstance = true;

            return this;
        }

        public MojViewPropBuilder Rows(int value)
        {
            if (GetTargetPropType(Prop).AnnotationDataType != DataType.MultilineText)
                throw new MojenException("The directive 'Row' can only be used on multiline text properties.");

            Prop.RowCount = value;

            return this;
        }

        MojPropType GetTargetPropType(MojViewProp vprop)
        {
            if (!vprop.FormedNavigationTo.Is)
                return vprop.Type;

            return vprop.FormedNavigationTo.TargetProp.Type;
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

        public MojViewPropBuilder AutoFitColumn()
        {
            Prop.IsColAutofit = true;
            return this;
        }

        public MojViewPropBuilder MaxWidth(int width)
        {
            Prop.MaxWidth = width;
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
            return AsCode("html");
        }

        public MojViewPropBuilder AsScss()
        {
            return AsCode("scss");
        }

        MojViewPropBuilder AsCode(string type)
        {
            if (!Prop.Type.IsString)
                throw new MojenException($"'AsCode' is applicable to string properties only.");

            Prop.UseCodeRenderer = type;
            return this;
        }

        public MojViewPropBuilder RenderHtml()
        {
            Prop.IsRenderedHtml = true;
            return this;
        }

        public MojViewPropBuilder Distinct()
        {
            Prop.IsLookupDistinct = true;

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

        public MojViewPropBuilder NoAutocomplete()
        {
            Prop.IsAutocomplete = false;

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

        public MojViewPropBuilder CascadeFrom(MojFormedType type, bool deactivatable = false,
            string fromPropDisplay = null,
            string title = null)
        {
            if (!Prop.IsSelector)
                throw new MojenException($"Cascade from is allowed for selectors only.");

            if (!TypeConfig.IsAccessibleFromThis(type))
                throw new MojenException($"Type '{type._Type.ClassName}' cannot be accessed from type '{TypeConfig.ClassName}'.");

            if (type.FormedNavigationFrom.Steps.Count > 1)
                throw new MojenException($"Cascade from is allowed for direct properties only.");

            if (!Prop.CascadeFrom.Is)
                Prop.CascadeFrom = new MojCascadeFromConfigCollection();

            Prop.CascadeFrom.Items.Add(new MojCascadeFromConfig
            {
                FromType = type,
                FromPropDisplayName = fromPropDisplay,
                IsDeactivatable = deactivatable,
                Title = title
            });

            return this;
        }

        public MojViewPropBuilder CascadeFromScope(MojProp source, MojProp target)
        {
            if (!Prop.Lookup.Is || !Prop.CascadeFrom.Is)
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
        public MojViewPropBuilder Lookup(string group = null,
            string viewAlias = null,
            string viewId = null,
            Action<MexConditionBuilder> filter = null)
        {
            Prop.IsSelector = true;
            Prop.Lookup = new MojLookupViewPropConfig
            {
                Is = true,
                TargetType = Prop.FormedNavigationTo.TargetType,
                ViewGroup = group,
                ViewAlias = viewAlias,
                ViewId = viewId,
                QueryFilter = MexConditionBuilder.BuildCondition(filter, required: false)
            };

            return this;
        }

        public MojViewPropBuilder Selector()
        {
            Prop.IsSelector = true;

            return this;
        }

        public MojViewPropBuilder LoggedInPerson()
        {
            Prop.IsLoggedInPerson = true;

            return this;
        }

        public MojViewPropBuilder InputOnly()
        {
            Prop.IsInputOnly = true;

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