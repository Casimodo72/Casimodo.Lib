﻿using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    public class MojColumnDefinition
    {
        public virtual void Build()
        { }
    }

    public static class WebStyleExtensions
    {
        public static string GetGroupLabelStyle(this ViewTemplateItem item)
        {
            return item.GetGroupStyle()?.GetLabelContainerClass?.Invoke();
        }

        public static string GetGroupPropStyle(this ViewTemplateItem item)
        {
            return item.GetGroupStyle()?.GetPropContainerClass?.Invoke();
        }

        public static Style GetGroupStyle(this ViewTemplateItem item)
        {
            return item.Parent.Style as Style;
        }
    }

    public class Style : MojColumnDefinition
    {
        public string Col { get; set; }

        public Func<string> GetLabelContainerClass { get; set; }
        public Func<string> GetPropContainerClass { get; set; }

        public string BackColor { get; set; }

        public int? Width { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
        public int? MinMaxWidth
        {
            get { return _minMaxWidth; }
            set
            {
                _minMaxWidth = value;
                MinWidth = value;
                MaxWidth = value;
            }
        }
        int? _minMaxWidth;

        static readonly List<string> _cols =
        [
            "lg-", "md-", "sm-", "xs-"
        ];

        public override void Build()
        {
            if (Col == null)
                return;

            var sb = new StringBuilder();
            int i = 0;
            var tokens = Col.Split(' ');
            foreach (var cur in Col.Split(' '))
            {
                if (i++ > 0) sb.o(" ");
                if (_cols.Any(x => cur.StartsWith(x)))
                    sb.o("col-" + cur);
                else
                    sb.o(cur);
            }

            Col = sb.ToString().TrimEnd();
        }
    }

    public class MojControllerViewBuilder : MojViewBuilder
    {
        public MojControllerViewBuilder(MojControllerBuilder controller, MojViewConfig view)
            : base(view)
        {
            ControllerBuilder = controller;
        }

        public MojControllerBuilder ControllerBuilder { get; private set; }
    }

    // KABU TODO: We might need to move the *content* building methods into a seperate class.
    //     E.g. into "MojViewContentBuilder".
    public class MojViewBuilder
    {
        public MojViewBuilder(MojViewConfig view)
        {
            View = view;
        }

        public MojViewConfig View { get; private set; }

        public MojViewConfig Build()
        {
            // Initial sort props
            // If none explicitely defined, then use the initial sort configuration of the MojType.
            if (!View.Props.Any(x => x.InitialSort.Is))
            {
                foreach (var prop in View.TypeConfig.GetProps().Where(x => x.InitialSort.Is))
                {
                    var vprop = View.Props.FirstOrDefault(x => x.Name == prop.Name);
                    if (vprop != null)
                        vprop.InitialSort = prop.InitialSort;
                }
            }

            // Add and assign color properties if needed.
            foreach (var vprop in View.Props
                .Where(x => x.OrigTargetProp?.UseColor == true && x.ColorProp == null)
                .ToArray())
            {
                MojProp colorProp = null;
                // KABU TODO: MAGIC hack by prop name "Color".
                if (vprop.OrigTargetProp.FormedNavigationFrom.Is)
                    colorProp = vprop.OrigTargetProp.FormedNavigationFrom.Last.TargetFormedType.Get("Color");
                else
                    colorProp = vprop.OrigTargetProp.DeclaringType.GetProps().First(x => x.Name == "Color");

                var vcolorProp = View.Props.FirstOrDefault(x => x.OrigTargetProp?.FormedTargetPath == colorProp.FormedTargetPath);

                if (vcolorProp != null)
                    vprop.ColorProp = vcolorProp;
                else
                    // Add implicitely.
                    vprop.ColorProp = Prop(colorProp, hidden: true).Prop;
            }

            OnNamingChanged();

            return View;
        }

        public MojViewBuilder Use<T>(object args = null)
            where T : MojenGenerator
        {
            var use = MojenBuildExtensions.Use<T>(View.UsingGenerators, args);

            return this;
        }

        public MojViewBuilder OverrideOptions<T>(Action<T> build)
           where T : class, ICloneable
        {
            Guard.ArgNotNull(build, nameof(build));

            var info = GetOptions<T>();
            info.UsingGen.Args.Remove(info.Options);

            info.Options = (T)info.Options.Clone();

            build(info.Options);

            info.UsingGen.Args.Add(info.Options);

            return this;
        }

        class OptionsInfo<T> where T : class
        {
            public MojUsingGeneratorConfig UsingGen { get; set; }
            public T Options { get; set; }
        }

        OptionsInfo<T> GetOptions<T>()
            where T : class
        {
            var info = new OptionsInfo<T>();
            foreach (var gen in View.UsingGenerators)
            {
                info.UsingGen = gen;
                info.Options = gen.Args.OfType<T>().FirstOrDefault();
                if (info.Options != null)
                    return info;
            }

            throw new MojenException($"Options '{nameof(T)}' not found.");
        }

        public MojViewBuilder CustomView()
        {
            View.IsCustomView = true;
            return this;
        }

        public MojViewBuilder Id(string id)
        {
            View.Id = id;
            return this;
        }

        public bool IsEditor
        {
            get { return View.Kind.Roles.HasFlag(MojViewRole.Editor); }
        }

        public bool IsReadOnly
        {
            get { return !IsEditor; }
        }

        public virtual MojViewBuilder Page()
        {
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.Page;
            View.Kind.RawControllerAction = ActionName.Index;

            View.CanCreate = true;
            View.CanModify = true;
            View.CanDelete = true;

            Title(View.TypeConfig.DisplayPluralName);
            return this;
        }

        public virtual MojViewBuilder List()
        {
            View.IsPartial = true;
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.List;
            View.Kind.RawControllerAction = "List";

            View.CanCreate = true;
            View.CanModify = true;
            View.CanDelete = true;

            // NOTE: Has no action name.
            return this;
        }

        public MojViewBuilder CustomControllerAction(string name)
        {
            if (View.Group != null)
                throw new MojenException("The view can't have a custom action name if a group was specified.");

            View.Kind.RawControllerAction = null;
            View.CustomControllerActionName = name;

            OnNamingChanged();

            return this;
        }

        public MojViewBuilder SingleLookupView(params MojProp[] parameters)
        {
            View.Kind.Mode = MojViewMode.Read;

            // KABU TODO: ELIMINATE: Currently we need a hack to compensate for
            //   the issue that lookup views are also lists.
            //   There should be two separate views instead: one lookup view and its
            //   content would be the list view.
            View.Kind.Roles = MojViewRole.Lookup;

            View.Kind.RawControllerAction = ActionName.Lookup;
            View.Group = null;

            View.CanCreate = false;
            View.CanModify = false;
            View.CanDelete = false;

            // Dialogs are currently all modal and partial.
            View.IsModal = true;
            View.IsPartial = true;

            View.ItemSelection.IsEnabled = true;
            View.Lookup = new MojLookupViewConfig
            {
                Multiplicity = MojMultiplicity.OneOrZero,
                Parameters = new List<MojProp>(parameters)
            };
            // Currently all lookup views are defined as dialogs.
            View.IsDialog = true;

            Title(View.TypeConfig.DisplayPluralName);

            OnNamingChanged();

            return this;
        }

        public MojViewBuilder LocalData()
        {
            View.IsLocalData = true;
            View.IsCustomSave = true;

            return this;
        }

        public MojViewBuilder NoApi()
        {
            View.HasNoApi = true;

            return this;
        }

        public MojViewBuilder Standalone(bool value = true)
        {
            if (View.Standalone.Is != value)
                View.Standalone = value ? new MojStandaloneViewConfig() : MojStandaloneViewConfig.None;

            return this;
        }

        public MojViewBuilder ListDialog()
        {
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.List;
            // View.Kind.RoleName = "List";
            View.Kind.RawControllerAction = "List";
            View.Group = "Standalone";

            View.CanCreate = false;
            View.CanModify = false;
            View.CanDelete = false;

            // Dialogs are currently all modal and partial.
            View.IsModal = true;
            View.IsPartial = true;

            View.Standalone = new MojStandaloneViewConfig
            {
                // KABU TODO: REMOVE? Not used
                // Parameters = new List<MojProp>(parameters)
            };

            Title(View.TypeConfig.DisplayName);

            OnNamingChanged();

            return this;
        }

        /// <summary>
        /// Warning: This applies the filter to the original query URL.
        /// If a component is used where an overall filter is computed dynamically
        /// then this will fail, because it will be overriden or will produce
        /// errors if the computing mechanism does not expect a filter on the original query URL.
        /// </summary>
        public MojViewBuilder RiskySelectFilter(string filterExpression)
        {
            View.CustomSelectFilter = filterExpression;
            return this;
        }

        void OnNamingChanged()
        {
            View.Url = "/" + View.TypeConfig.PluralName;
            if (!string.IsNullOrEmpty(View.ControllerActionName) && View.ControllerActionName != "Index")
                View.Url += "/" + View.ControllerActionName;
        }

        public virtual MojViewBuilder Details()
        {
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.Details;
            View.Kind.RawControllerAction = ActionName.Details;

            View.CanCreate = false;
            View.CanModify = false;
            View.CanDelete = false;

            Title(View.TypeConfig.DisplayName);
            OnNamingChanged();

            return this;
        }

        public virtual MojViewBuilder Editor(params MojProp[] parameters)
        {
            View.Kind.Mode = MojViewMode.Create | MojViewMode.Update;
            View.Kind.Roles = MojViewRole.Editor;
            View.Kind.RawControllerAction = ActionName.Editor;

            View.CanCreate = false;
            View.CanModify = false;
            View.CanDelete = false;

            if (parameters != null)
                View.Parameters.AddRange(parameters);

            Title(View.TypeConfig.DisplayName);
            OnNamingChanged();

            return this;
        }

        public virtual MojViewBuilder Custom(string part, MojViewRole role, params MojProp[] parameters)
        {
            View.CustomPartName = part;
            View.Kind.Mode = MojViewMode.None;
            View.Kind.Roles = role;
            View.Kind.RawControllerAction = role.ToString();

            if (parameters != null)
                View.Parameters.AddRange(parameters);

            Title(View.TypeConfig.DisplayName);
            OnNamingChanged();

            return this;
        }

        public MojViewBuilder UseTemplate(Action<ViewTemplate> config)
        {
            config(View.Template);

            return this;
        }

        public MojViewBuilder UseMVVM()
        {
            View.UseMVVM = true;
            return this;
        }

        public MojViewBuilder NavigatableTo()
        {
            View.IsNavigatableTo = true;
            return this;
        }

        public MojViewBuilder Filterable(bool value = true)
        {
            View.IsFilterable = value;

            return this;
        }

        public MojViewBuilder Auth(bool value = true, bool cascade = true, bool? overwrite = null)
        {
            View.IsAuthEnabled = value;
            View.IsAuthAmbientForGroup = cascade;
            if (overwrite != null)
                View.IsAuthAmbientOverwritten = overwrite.Value;

            return this;
        }

        public MojViewBuilder AuthRole(string role, string permit = "*", string deny = null)
        {
            View.AuthPermissions.Add(new MojAuthPermission
            {
                Role = role,
                Permit = permit,
                Deny = deny
            });

            return this;
        }

        public MojViewBuilder Selectable(bool multi = false, bool checkBox = false, bool allCheckBox = false)
        {
            View.ItemSelection.IsEnabled = true;
            View.ItemSelection.IsMultiselect = multi;
            View.ItemSelection.UseCheckBox = checkBox;
            View.ItemSelection.UseAllCheckBox = allCheckBox;
            return this;
        }

        public MojViewBuilder GlobalCompanyFilter(bool value = true)
        {
            View.IsGlobalCompanyFilterEnabled = value;
            return this;
        }

        public MojViewBuilder CompanyFilter(bool value = true)
        {
            View.IsCompanyFilterEnabled = value;
            return this;
        }

        public MojViewBuilder TagsFilter(bool value = true)
        {
            View.IsTagsFilterEnabled = value;
            return this;
        }

        public MojViewBuilder ClientCached()
        {
            View.IsCachedOnClient = true;
            return this;
        }

        public MojViewBuilder SimpleFilter(Action<MexConditionBuilder> condition)
        {
            var expression = BuildCondition(condition);
            if (expression.IsEmpty)
                return this;

            View.SimpleFilter = expression;

            return this;
        }

        MexExpressionNode BuildCondition(Action<MexConditionBuilder> build)
        {
            Guard.ArgNotNull(build, nameof(build));

            return MexConditionBuilder.BuildCondition(build);
        }

        public MojViewBuilder FilterByLoggedInPerson(MojFormedType personProp)
        {
            Guard.ArgNotNull(personProp, nameof(personProp));

            if (personProp.FormedNavigationFrom.Steps.Count != 1)
                throw new ArgumentException("The property must be a direct property of the type.");

            var prop = personProp.FormedNavigationFrom.RootProp;
            if (!prop.Reference.Is)
                throw new ArgumentException("The property is not a reference property.");

            prop = prop.ForeignKey;
            if (prop == null)
                throw new ArgumentException("The property has no foreign key.");

            CheckIsAccessibleFromThis(prop);

            View.IsFilteredByLoggedInPerson = true;
            View.FilteredByLoogedInPersonProp = prop.Name;

            return this;
        }

        public MojViewBuilder Partial()
        {
            View.IsPartial = true;
            return this;
        }

        public MojViewBuilder Inline()
        {
            View.IsInline = true;
            return this;
        }

        public MojViewBuilder Modal(bool value = true)
        {
            View.IsModal = value;
            return this;
        }

        public MojViewBuilder Dialog(bool value = true)
        {
            View.IsDialog = value;
            return this;
        }

        public MojViewBuilder VirtualFilePath(string path)
        {
            View.VirtualFilePath = path;
            return this;
        }

        public MojViewBuilder CustomODataCreate()
        {
            View.IsCustomODataCreateAction = true;
            return this;
        }

        public MojViewBuilder CustomODataUpdate(bool action = false, bool updateMask = false)
        {
            View.IsCustomODataUpdateAction = !action;
            View.IsCustomODataUpdateData = !updateMask;
            return this;
        }

        public MojViewBuilder Maximize()
        {
            View.IsMaximized = true;
            return this;
        }

        public MojViewBuilder Width(int width)
        {
            View.Width = width;
            return this;
        }

        public MojViewBuilder MinWidth(int width)
        {
            View.MinWidth = width;
            return this;
        }

        public MojViewBuilder MaxWidth(int width)
        {
            View.MaxWidth = width;
            return this;
        }

        // TODO: REMOVE?
        //public MojViewBuilder CustomCommand(string name, string displayName)
        //{
        //    View.CustomCommands.Add(new MojViewCommand
        //    {
        //        Name = name,
        //        DisplayName = displayName
        //    });

        //    return this;
        //}

        // TODO: REMOVE?
        //public MojViewBuilder ListItemCommand(string name, string displayName)
        //{
        //    View.ListItemCommands.Add(new MojViewCommand
        //    {
        //        Name = name,
        //        DisplayName = displayName
        //    });

        //    return this;
        //}

        // TODO: REMOVE?
        //public MojViewBuilder RemoveListItemCommand(string name)
        //{
        //    var cmd = View.ListItemCommands.FirstOrDefault(x => x.Name == name);
        //    if (cmd != null)
        //        View.ListItemCommands.Remove(cmd);

        //    return this;
        //}

        public MojViewBuilder Content(Action<MojViewBuilder> build)
        {
            build(this);
            Build();

            return this;
        }

        public MojViewBuilder Content(Func<MojViewBuilder, MojViewBuilder> build)
        {
            build(this);

            return this;
        }

        public MojViewBuilder Configure(Func<MojViewBuilder, MojViewBuilder> configure)
        {
            configure(this);

            return this;
        }

        public MojViewBuilder Taggable(bool value = true)
        {
            if (View.IsTaggable == value)
                return this;

            View.IsTaggable = value;

            return this;
        }

        public MojViewBuilder MinHeight(int height)
        {
            View.MinHeight = height;
            return this;
        }

        /// <summary>
        /// The view is custom and will not be generated.
        /// </summary>        
        public MojViewBuilder Custom()
        {
            View.IsCustom = true;
            return this;
        }

        /// <summary>
        /// The view's controller method is custom and will not be generated.
        /// </summary>
        public MojViewBuilder CustomViewControllerMethod()
        {
            View.IsCustomViewControllerMethod = true;
            return this;
        }

        public MojViewBuilder Escape(bool escape)
        {
            View.IsEscapingNeeded = escape;
            return this;
        }

        public MojViewBuilder CreateAction()
        {
            View.Kind.Mode = MojViewMode.Create;
            View.Kind.Roles = MojViewRole.Editor;
            return this;
        }

        public MojViewBuilder Title(string title)
        {
            View.Title = title;

            return this;
        }

        public MojViewBuilder Reloadable(bool value = true)
        {
            View.IsReloadable = value;

            return this;
        }

        public MojViewBuilder NotExportable()
        {
            View.IsExportableToPdf = false;
            View.IsExportableToExcel = false;

            return this;
        }

        public MojViewBuilder Exportable(bool pdf = true, bool excel = true)
        {
            View.IsExportableToPdf = pdf;
            View.IsExportableToExcel = excel;

            return this;
        }


        public MojViewBuilder FileName(string name)
        {
            if (View.FileName != null)
                throw new MojenException("The view's file name was already assigned.");

            View.FileName = name;

            return this;
        }

        public MojViewBuilder Alias(string alias)
        {
            View.Alias = alias;
            return this;
        }

        public MojViewBuilder ListComponentId(string listComponentId)
        {
            View.ListComponentId = listComponentId;
            return this;
        }

        public MojViewBuilder Group(string name)
        {
            if (View.CustomControllerActionName != null)
                throw new MojenException("The view must not be in a group if a custom action name was specified.");

            View.Group = name;

            OnNamingChanged();

            return this;
        }

        void CheckViewId(MojViewConfig view = null)
        {
            if (string.IsNullOrWhiteSpace(View.Id))
                throw new MojenException($"The view for '{View.TypeConfig.Name}' (roles: {view.Kind.Roles}) has no ID.");

            if (view != null && string.IsNullOrWhiteSpace(view.Id))
                throw new MojenException($"The view for '{view.TypeConfig.Name}' (roles: {view.Kind.Roles}) has no ID.");
        }

        public MojViewBuilder Editor(Func<MojControllerBuilder, MojViewBuilder> build)
        {
            var vbuilder = build(((MojControllerViewBuilder)this).ControllerBuilder);

            return Editor(vbuilder);
        }

        public MojViewBuilder Editor(MojViewBuilder vbuilder)
        {
            return Editor(vbuilder.Build());
        }

        public MojViewBuilder Editor(MojViewConfig view)
        {
            Guard.ArgNotNull(view, nameof(view));

            if (view.Kind.Roles != MojViewRole.Editor)
                throw new MojenException("The view must be an editor.");

            if (view.Group != View.Group)
                throw new MojenException("The editor must not be in a different group.");

            View.EditorView = view;

            EnsureEditAuthControlPropsIfMissing();

            return this;
        }

        public MojViewBuilder EnsureEditAuthControlPropsIfMissing()
        {
            // KABU TODO: IMPORTANT: Make this configurable for consumer.
            // Add mandatory IsReadOnly and IsNotDeletable props.
            AddCommandPredicatePropIfMissing("IsReadOnly");
            AddCommandPredicatePropIfMissing("IsNotDeletable");

            return this;
        }

        MojViewBuilder AddCommandPredicatePropIfMissing(string name)
        {
            var prop = View.TypeConfig.FindProp(name);
            if (prop != null && !View.Props.Any(x => x.FormedTargetPath == prop.Name))
                Prop(prop, hidden: true, readOnly: true);

            return this;
        }

        public MojViewBuilder InlineDetails(Func<MojControllerBuilder, MojViewBuilder> build)
        {
            var vbuilder = build(((MojControllerViewBuilder)this).ControllerBuilder);

            return InlineDetails(vbuilder);
        }

        public MojViewBuilder InlineDetails(MojViewBuilder vbuilder)
        {
            return InlineDetails(vbuilder.Build());
        }

        public MojViewBuilder InlineDetails(MojViewConfig view)
        {
            if (view.Group != View.Group)
                throw new MojenException("The inline details view must not be in a different group.");

            View.InlineDetailsView = view;

            return this;
        }

        public MojViewBuilder NoView()
        {
            View.NoView = true;

            return this;
        }

        public MojViewBuilder Single()
        {
            View.IsSingle = true;

            return this;
        }

        public MojViewBuilder Mobile()
        {
            View.IsForMobile = true;

            return this;
        }

        public MojViewBuilder Desktop()
        {
            View.IsForDesktop = true;

            return this;
        }

        public MojViewBuilder Message(string message)
        {
            View.Message = message;
            return this;
        }

        public MojViewBuilder CanCreate(bool value = true)
        {
            View.CanCreate = value;
            return this;
        }

        public MojViewBuilder CanEdit(bool value = true)
        {
            View.CanModify = value;
            return this;
        }

        public MojViewBuilder CustomSave()
        {
            View.IsCustomSave = true;
            return this;
        }

        public MojViewBuilder CustomSaveApi()
        {
            View.IsCustomSaveApi = true;
            return this;
        }

        public MojViewBuilder CanDelete(bool value = true)
        {
            View.CanDelete = value;
            return this;
        }

        public ViewTemplate IncludeCustomView(string name, MojViewMode showOn = MojViewMode.All)
        {
            return View.Template.CustomView(name, showOn);
        }

        public ViewTemplate Label(string label = null, string tag = null)
        {
            var result = View.Template.Label(label);
            if (tag != null)
                result.Cur._tag = tag;

            return result;
        }

        public void EndRun()
        {
            View.Template.EndRun();
        }

        public MojViewBuilder ListItemSelectorCheckBox()
        {
            return CheckBox(subType: "ListItemSelectorCheckBox");
        }

        public MojViewBuilder CheckBox(string subType = null, string headerCssClass = null)
        {
            var index = View.Props.Count;

            View.CustomControls.Add(new MojViewCustomControl
            {
                Index = index,
                Type = "CheckBox",
                SubType = subType,
                HeaderCssClass = headerCssClass
            });

            return this;
        }

        public MojViewCollectionPropBuilder ListProp(MojFormedType type)
        {
            // Check for collection prop.
            var prop = type.FormedNavigationFrom.Last.SourceProp;
            if (!prop.Reference.IsToMany)
                throw new MojenException("The given property must be a collection property.");

            if (!View.TypeConfig.IsNativeProp(prop))
                throw new MojenException($"The collection property '{prop}' is not a member of type '{View.TypeConfig.ClassName}'.");

            var path = new MojFormedNavigationPath
            {
                Steps = []
            };
            path.AddStep(new MojFormedNavigationPathStep
            {
                SourceType = View.TypeConfig,
                SourceProp = prop,
                TargetType = prop.Reference.ToType,
                //TargetProp = prop
            });
            path.Build();
            path.IsForeign = false;

            // KABU TODO: REMOVE: We can't do that because there are cases
            //  when we want the same property to appear twice.
#if (false)
            // If an already added property is requested, then return that.
            MojViewProp existingProp = View.FindSameProp(prop);
            if (existingProp != null)
                return MojViewPropBuilder.Create(this, existingProp);
#endif

            // Add view for the collection item.

            var collectionPropBuilder = MojViewCollectionPropBuilder.Create(this, type, prop);
            var vprop = collectionPropBuilder.Prop;
            vprop.FormedNavigationTo = path;

            // Add collection prop to view.
            View.Props.Add(vprop);
            vprop.Position = View.Props.Count;
            // Add collection prop to view template.
            View.Template.Label(vprop);
            View.Template.o(vprop);
            View.Template.EndRun();

            return collectionPropBuilder;
        }

        /// <summary>
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="hidden">Property will be fetched but is not part of the view and the data update mask.</param>
        /// <param name="readOnly"></param>
        /// <param name="external">Intended to be edited via a user-provided custom template.</param>
        public MojViewPropBuilder Prop(MojProp prop, bool hidden = false, bool? readOnly = null, bool external = false, bool label = true)
        {
            if (readOnly == null)
            {
                // Hidden props are read-only by default.
                readOnly = hidden ? true : false;
            }

            var pbuilder = SimplePropCore(prop, readOnly: readOnly.Value, hidden: hidden);

            pbuilder.Prop.NoLabel = !label;

            // View props are sortable by default.
            pbuilder.Prop.IsSortable = true;

            if (hidden || external)
            {
                if (hidden)
                {
                    pbuilder.Prop.HideModes = MojViewMode.All;

                    if (readOnly.Value)
                        pbuilder.ReadOnly();
                }

                // KABU TODO: Why do I set IsExternal for all hidden properties?
                pbuilder.Prop.IsExternal = external;
            }
            else
            {
                if (label)
                    View.Template.Label(pbuilder.Prop);
                View.Template.o(pbuilder.Prop);
                if (label)
                    View.Template.EndRun();
            }

            return pbuilder;
        }

        internal MojViewPropBuilder SimplePropCore(MojProp prop, bool readOnly = false, bool hidden = false)
        {
            CheckNotForeignKeyProp(prop, hidden);

            var pbuilder = CreateSimpleViewProp(prop);

            View.Props.Add(pbuilder.Prop);
            pbuilder.Prop.Position = View.Props.Count;

            if (readOnly)
                pbuilder.ReadOnly();

            return pbuilder;
        }

        void CheckIsAccessibleFromThis(MojProp prop)
        {
            if (!View.TypeConfig.IsAccessibleFromThis(prop))
                throw new MojenException($"Property '{prop}' cannot be accessed from type '{View.TypeConfig.ClassName}'.");
        }

        /// <summary>
        /// Creates a view property based on the given property.
        /// </summary>
        MojViewPropBuilder CreateSimpleViewProp(MojProp prop)
        {
            CheckIsAccessibleFromThis(prop);

            var path = prop.FormedNavigationFrom;

            MojProp effectiveProp = prop;

            if (View.TypeConfig.IsForeign(prop))
            {
                effectiveProp = prop.FormedNavigationFrom.Root.SourceProp;
            }

            // KABU TODO: REMOVE: We can't do that because there are cases
            //  when we want the same property to appear twice.
#if (false)
            // If an already added property is requested, then return that.
            MojViewProp existingProp = View.FindSameProp(prop);
            if (existingProp != null)
                return MojViewPropBuilder.Create(this, existingProp);
#endif

            var pbuilder = MojViewPropBuilder.Create(this, effectiveProp);
            pbuilder.Prop.FormedNavigationTo = path;
            if (pbuilder.Prop.FormedNavigationFrom.Is)
                throw new Exception();
            pbuilder.Prop.OrigTargetProp = prop;

            return pbuilder;
        }

        void CheckNotForeignKeyProp(MojProp prop, bool hidden)
        {
            if (prop.IsForeignKey && !hidden)
            {
                if (!prop.FileRef.Is && !View.IsCustom)
                    throw new MojenException("Non-hidden foreign key properties must not be used when building views.");
            }
        }

        public MojViewBuilder Grid(Action content)
        {
            View.Template.AnyGroup("grid");
            content();
            View.Template.EndGroup();
            return this;
        }

        public MojViewBuilder Column(Action content)
        {
            return Column(null, content);
        }

        public MojViewBuilder Column(MojColumnDefinition col, Action content)
        {
            View.Template.AnyGroup("column", col: col);
            content();
            View.Template.EndGroup();
            return this;
        }

        public MojViewBuilder Row(int row, Action content)
        {
            var item = View.Template.AnyGroup("row", index: row);
            content();
            View.Template.EndGroup(item);
            return this;
        }

        public MojViewBuilder ShowIf(MojFormedType prop)
        {
            View.Template.Cur.VisibilityCondition = prop;

            return this;
        }

        public MojViewBuilder ShowOn(MojViewMode mode)
        {
            View.Template.Cur.VisibilityCondition = MojViewMode.All & ~mode;

            return this;
        }

        public MojViewBuilder GroupBox(string header, Action content)
        {
            return GroupBoxFor(group: null, header: header, content: content);
        }

        public MojViewBuilder GroupBox(string header, MojColumnDefinition col, Action content)
        {
            return GroupBoxCore(group: null, header: header, col: col, content: content);
        }

        public MojViewBuilder GroupBoxFor(object group, string header, Action content)
        {
            return GroupBoxCore(group: group, header: header, col: null, content: content);
        }

        MojViewBuilder GroupBoxCore(object group, string header, MojColumnDefinition col, Action content)
        {
            var item = View.Template.AnyGroup("group-box", group: group, header: header, col: col);
            content();
            View.Template.EndGroup(item);
            return this;
        }
    }
}