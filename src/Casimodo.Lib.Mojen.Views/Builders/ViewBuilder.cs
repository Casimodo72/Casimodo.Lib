using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public class MojColumnDefinition
    {
        public virtual void Build()
        { }
    }

    public class Style : MojColumnDefinition
    {
        public string Col { get; set; }

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

        static readonly List<string> _cols = new List<string>
        {
            "lg-", "md-", "sm-", "xs-"
        };

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

        public override MojViewBuilder Editor()
        {
            base.Editor();

            return this;
        }
    }

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

            OnNamingChanged();

            return View;
        }

        public MojViewBuilder Use<T>(object args = null)
            where T : MojenGenerator
        {
            var use = MojenBuildExtensions.Use<T>(View.UsingGenerators, args);

            return this;
        }

        public MojViewBuilder Viewless()
        {
            View.IsViewless = true;
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
            View.Kind.Roles = MojViewRole.Page | MojViewRole.List;
            View.Kind.RoleName = ActionName.Index;
            View.Kind.RawAction = ActionName.Index;

            View.CanCreate = true;
            View.CanEdit = true;
            View.CanDelete = true;

            Title(View.TypeConfig.DisplayPluralName);
            return this;
        }

        public virtual MojViewBuilder List()
        {
            View.IsPartial = true;
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.List;
            View.Kind.RoleName = "List";

            View.CanCreate = true;
            View.CanEdit = true;
            View.CanDelete = true;

            // NOTE: Has no action name.
            return this;
        }

        // KABU TODO: REMOVE: Never used.
        //public MojViewBuilder Name(string name)
        //{
        //    View.Name = name;
        //    OnViewNameChanged();

        //    return this;
        //}

        public MojViewBuilder CustomAction(string name)
        {
            if (View.Group != null)
                throw new MojenException("The view can't have a custom action name if a group was specified.");
            View.Kind.RawAction = null;

            View.CustomControllerActionName = name;

            OnNamingChanged();

            return this;
        }

        public MojViewBuilder ComponentRole(string name)
        {
            View.Kind.RoleName = name;
            OnNamingChanged();

            return this;
        }

        public MojViewBuilder LookupSingle(params MojProp[] parameters)
        {
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.Lookup | MojViewRole.List;
            //View.Kind.ActionName = "Lookup" + View.TypeConfig.Name;
            View.Kind.RawAction = ActionName.Lookup;
            View.Kind.RoleName = MojViewRole.Lookup.ToString();
            View.Group = null; // "Lookup";

            View.CanCreate = false;
            View.CanEdit = false;
            View.CanDelete = false;

            // Dialogs are currently all modal and partial.
            View.IsModal = true;
            View.IsPartial = true;

            View.ItemSelection.IsEnabled = true;
            View.Lookup = new MojLookupViewConfig
            {
                Is = true,
                Multiplicity = Data.MojMultiplicity.OneOrZero,
                Parameters = new List<MojProp>(parameters)
            };

            Title(View.TypeConfig.DisplayPluralName);

            OnNamingChanged();

            return this;
        }

        public MojViewBuilder Standalone() // KABU TODO: REMOVE? Not used: params MojProp[] parameters)
        {
            if (View.Standalone.Is)
                throw new MojenException("This view is already standalone.");

            View.Standalone = new MojStandaloneViewConfig { Is = true };

            return this;
        }

        public MojViewBuilder ListDialog()  // KABU TODO: REMOVE? Not used: params MojProp[] parameters)
        {
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.List;
            View.Kind.RoleName = "List";
            View.Kind.RawAction = "List";
            View.Group = "Standalone";

            View.CanCreate = false;
            View.CanEdit = false;
            View.CanDelete = false;

            // Dialogs are currently all modal and partial.
            View.IsModal = true;
            View.IsPartial = true;

            View.Standalone = new MojStandaloneViewConfig
            {
                Is = true,
                // KABU TODO: REMOVE? Not used
                //Parameters = new List<MojProp>(parameters)
            };

            Title(View.TypeConfig.DisplayName);

            OnNamingChanged();

            return this;
        }

        public MojViewBuilder SelectFilter(string filterExpression)
        {
            View.CustomSelectFilter = filterExpression;
            return this;
        }

        void OnNamingChanged()
        {
            View.Url = $"/{View.TypeConfig.PluralName}/{View.ControllerActionName}";
        }

        public virtual MojViewBuilder Details()
        {
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.Details;
            View.Kind.RoleName = ActionName.Details;
            View.Kind.RawAction = ActionName.Details;

            View.CanCreate = false;
            View.CanEdit = false;
            View.CanDelete = false;

            Title(View.TypeConfig.DisplayName);
            OnNamingChanged();

            return this;
        }

        public virtual MojViewBuilder Editor()
        {
            View.Kind.Mode = MojViewMode.Create | MojViewMode.Update;
            View.Kind.Roles = MojViewRole.Editor;
            View.Kind.RoleName = "Editor";
            View.Kind.RawAction = ActionName.Edit;

            View.CanCreate = false;
            View.CanEdit = false;
            View.CanDelete = false;

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

        public MojViewBuilder SingleFilterable()
        {
            View.IsGuidFilterable = true;
            return this;
        }

        public MojViewBuilder Filterable(bool value = true)
        {
            View.IsFilterable = value;

            return this;
        }

        // KABU TODO: REMOVE: 
        //public MojViewBuilder CustomActionToggle(string name, string display, bool value, bool visible = true)
        //{
        //    View.CustomActions.Add(new MojViewActionConfig
        //    {
        //        Name = name,
        //        DisplayName = display,
        //        Kind = MojViewActionKind.Toggle,
        //        DefaultValue = value,
        //        IsVisible = visible
        //    });
        //    return this;
        //}

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
            View.Permissions.Add(new MojAuthPermission
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

            //var pbuilder = CreateSimpleViewProp(personProp);
            //pbuilder.ReadOnly();
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

        public MojViewBuilder Modal()
        {
            View.IsModal = true;
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

        /// <summary>
        /// KABU TODO: ELIMINATE: This is a temporary hack for the generation of
        /// factories for JS component spaces, because previously such component spaces
        /// were page singletons, but now we need multiple of such components on a page.
        /// It was a mistake to design all JS component spaces as singletons. For
        /// lookup views it makes sense but no for all components.
        /// </summary>
        /// <returns></returns>
        public MojViewBuilder Factory()
        {
            View.HasFactory = true;

            return this;
        }

        public MojViewBuilder Group(string name)
        {
            if (View.CustomControllerActionName != null)
                throw new MojenException("The view must not be in a group if a custom action name was specified.");

            View.Group = name;

            // KABU TODO: REMOVE
            //if (!View.Kind.IsCustomActionName &&
            //     View.Kind.Roles.HasFlag(MojViewRole.Lookup))
            //{
            //    View.Kind.ActionName = (View.Group ?? "") + "Lookup" + View.TypeConfig.Name;
            //}

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
            // Add mandatory IsReadOnly and IsDeletable props.
            AddCommandPredicatePropIfMissing("IsReadOnly");
            AddCommandPredicatePropIfMissing("IsDeletable");

            return this;
        }

        MojViewBuilder AddCommandPredicatePropIfMissing(string name)
        {
            var prop = View.TypeConfig.FindProp(name);
            if (prop != null && !View.Props.Any(x => x.Name == prop.Name))
                Prop(prop, hidden: true);

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
                throw new MojenException("The editor must not be in a different group.");

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
            View.CanEdit = value;
            return this;
        }

        public MojViewBuilder CanDelete(bool value = true)
        {
            View.CanDelete = value;
            return this;
        }

        public ViewTemplate UseCustomView(string name, MojViewMode showOn = MojViewMode.All)
        {
            return View.Template.CustomView(name, showOn);
        }

        public ViewTemplate Label(string label = null)
        {
            return View.Template.Label(label);
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
                Steps = new List<MojFormedNavigationPathStep>()
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
        /// <returns></returns>
        public MojViewPropBuilder Prop(MojProp prop, bool hidden = false, bool? readOnly = null, bool external = false)
        {
            var pbuilder = SimplePropCore(prop, readOnly: readOnly ?? false, hidden: hidden);

            if (hidden || external)
            {
                if (hidden)
                {
                    pbuilder.Prop.HideModes = MojViewMode.All;

                    // Hidden props are read-only by default, except for if explicitely non-read-only.
                    if (readOnly != false)
                        pbuilder.ReadOnly();
                }

                // KABU TODO: Why do I set IsExternal for all hidden properties?
                pbuilder.Prop.IsExternal = external;
            }
            else
            {
                View.Template.Label(pbuilder.Prop);
                View.Template.o(pbuilder.Prop);
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

            if (View.TypeConfig.IsForeign(prop))
            {

                // Get and clone root property of navigation path, which is the native property of the current MojType.

                // NOTE: This will create multiple clones of the same navigation property
                //   if the navigation property is used to access different foreign properties.
                // NOTE: Thus, elsewhere: group by property name never by property instance.
                //var nativeProp = prop.FormedNavigationFrom.Root.SourceProp.Clone();

                // KABU TODO: Why do I set FormedNavigationTo on the type's property and not only on the view-property?
                //nativeProp.FormedNavigationTo = prop.FormedNavigationFrom;
                //prop = nativeProp;

                prop = prop.FormedNavigationFrom.Root.SourceProp;
            }

            // KABU TODO: REMOVE: We can't do that because there are cases
            //  when we want the same property to appear twice.
#if (false)
            // If an already added property is requested, then return that.
            MojViewProp existingProp = View.FindSameProp(prop);
            if (existingProp != null)
                return MojViewPropBuilder.Create(this, existingProp);
#endif

            var pbuilder = MojViewPropBuilder.Create(this, prop);
            pbuilder.Prop.FormedNavigationTo = path;

            return pbuilder;
        }

        void CheckNotForeignKeyProp(MojProp prop, bool hidden)
        {
            if (prop.Reference.IsForeignKey && !hidden)
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