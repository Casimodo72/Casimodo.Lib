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
        public MojControllerViewBuilder(ControllerBuilder controller, MojViewConfig view)
            : base(view)
        {
            Controller = controller;
        }

        public ControllerBuilder Controller { get; private set; }

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

            return View;
        }

        public MojViewBuilder Use<T>(object args = null)
            where T : MojenGenerator
        {
            var use = MojenBuildExtensions.Use<T>(View.UsingGenerators, args);

            return this;
        }

        public MojViewBuilder ViewModelOnly()
        {
            View.IsViewModelOnly = true;
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

        public virtual MojViewBuilder Index()
        {
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.Index | MojViewRole.List;
            View.Kind.ComponentRoleName = ActionName.Index;
            View.Kind.ActionName = ActionName.Index;

            Title(View.TypeConfig.DisplayPluralName);
            return this;
        }

        public virtual MojViewBuilder List()
        {
            View.IsPartial = true;
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.List;
            View.Kind.ComponentRoleName = "List";

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

        public MojViewBuilder Action(string name)
        {
            View.Kind.ActionName = name;
            View.Kind.IsCustomActionName = true;
            OnViewUrlChanged();

            return this;
        }

        public MojViewBuilder ComponentRole(string name)
        {
            View.Kind.ComponentRoleName = name;
            OnViewUrlChanged();

            return this;
        }

        public MojViewBuilder LookupSingle(params MojProp[] parameters)
        {
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.Lookup | MojViewRole.List;
            // KABU TODO: IMPORTANT: What if we have multipe lookups?
            View.Kind.ActionName = "Lookup" + View.TypeConfig.Name;
            View.Kind.ComponentRoleName = "Lookup";
            View.Group = "Lookup";

            // Dialogs are currently all modal and partial.
            View.IsModal = true;
            View.IsPartial = true;

            View.ItemSelection.IsEnabled = true;
            View.Lookup = new MojLookupViewConfig
            {
                Is = true,
                Cardinality = Data.MojCardinality.OneOrZero,
                Parameters = new List<MojProp>(parameters)
            };

            Title(View.TypeConfig.DisplayPluralName);

            OnViewUrlChanged();

            return this;
        }

        public MojViewBuilder StandaloneEditorDialog(params MojProp[] parameters)
        {
            View.Kind.Mode = MojViewMode.Update;
            View.Kind.Roles = MojViewRole.Editor;
            View.Kind.ComponentRoleName = "Editor";
            View.Kind.ActionName = ActionName.Edit;
            View.Group = "Standalone";

            // Dialogs are currently all modal and partial.
            View.IsModal = true;
            View.IsPartial = true;

            View.Standalone = new MojStandaloneViewConfig
            {
                Is = true,
                Parameters = new List<MojProp>(parameters)
            };

            Title(View.TypeConfig.DisplayName);

            OnViewUrlChanged();

            return this;
        }

        public MojViewBuilder SelectFilter(string filterExpression)
        {
            View.CustomSelectFilter = filterExpression;
            return this;
        }

        void OnViewUrlChanged()
        {
            View.Url = $"/{View.TypeConfig.PluralName}/{View.Kind.ActionName}";
        }

        public virtual MojViewBuilder Details()
        {
            View.Kind.Mode = MojViewMode.Read;
            View.Kind.Roles = MojViewRole.Details;
            View.Kind.ComponentRoleName = ActionName.Details;
            View.Kind.ActionName = ActionName.Details;
            Title(View.TypeConfig.DisplayName);
            return this;
        }

        public virtual MojViewBuilder Editor()
        {
            View.Kind.Mode = MojViewMode.Create | MojViewMode.Update;
            View.Kind.Roles = MojViewRole.Editor;
            View.Kind.ComponentRoleName = "Editor";
            View.Kind.ActionName = ActionName.Edit;
            Title(View.TypeConfig.DisplayName);
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

        public MojViewBuilder Auth(bool value = true)
        {
            View.IsAuthorizationNeeded = value;

            return this;
        }

        public MojViewBuilder Selectable(bool multi = false, bool checkBox = false)
        {
            View.ItemSelection.IsEnabled = true;
            View.ItemSelection.IsMultiselect = multi;
            View.ItemSelection.UseCheckBox = checkBox;
            return this;
        }

        public MojViewBuilder ClientCached()
        {
            View.IsCachedOnClient = true;
            return this;
        }

        public MojViewBuilder Partial()
        {
            View.IsPartial = true;
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
            View.FileName = name;

            return this;
        }

        public MojViewBuilder Group(string name)
        {
            View.Group = name;

            if (!View.Kind.IsCustomActionName &&
                 View.Kind.Roles.HasFlag(MojViewRole.Lookup))
            {
                View.Kind.ActionName = View.Group + "Lookup" + View.TypeConfig.Name;
                OnViewUrlChanged();
            }

            return this;
        }

        public MojViewBuilder EditorView(MojViewConfig view)
        {
            View.EditorView = view;

            if (view != null)
            {
                if (view.Kind.Roles != MojViewRole.Editor)
                    throw new MojenException("The given views must be an editor view.");

                // Add mandatory IsReadOnly and IsDeletable props.
                AddCommandPredicatePropIfMissing("IsReadOnly");
                AddCommandPredicatePropIfMissing("IsDeletable");
            }

            return this;
        }

        void AddCommandPredicatePropIfMissing(string name)
        {
            var prop = View.TypeConfig.FindProp(name);
            if (prop != null && !View.Props.Any(x => x.Name == prop.Name))
                Prop(prop, hidden: true);
        }

        public MojViewBuilder InlineDetailsView(MojViewConfig view)
        {
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

        public ViewTemplate UseCustomView(string name)
        {
            return View.Template.CustomView(name);
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

        public MojViewPropBuilder Prop(MojProp prop, bool hidden = false, bool external = false)
        {
            var pbuilder = PropCore(prop, hidden: hidden);

            if (hidden || external)
            {
                if (hidden)
                {
                    pbuilder.Prop.HideModes = MojViewMode.All;
                    pbuilder.ReadOnly();
                }

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

        internal MojViewPropBuilder PropCore(MojProp prop, bool readOnly = false, bool hidden = false)
        {
            if (prop.Reference.Is && prop.Reference.ForeignKey == prop && !hidden)
            {
                if (!prop.FileRef.Is && !View.IsCustom)
                    throw new MojenException("Foreign key properties must not be used when building views.");
            }

            if (!View.TypeConfig.IsAccessibleFromThis(prop))
                throw new MojenException($"Property '{prop}' cannot be accessed from type '{View.TypeConfig.ClassName}'.");

            if (View.TypeConfig.IsForeign(prop))
            {
                // Get and clone root property of navigation path.

                // NOTE: This will create multiple clones of the same navigation property
                //   if the navigation property is used to access different foreign properties.
                // NOTE: Thus, elsewhere: group by property name never by property instance.
                var naviProp = prop.FormedNavigationFrom.Root.SourceProp.Clone();

                naviProp.FormedNavigationTo = prop.FormedNavigationFrom;
                prop = naviProp;
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
            View.Props.Add(pbuilder.Prop);
            pbuilder.Prop.Position = View.Props.Count;

            if (readOnly)
                pbuilder.ReadOnly();

            return pbuilder;
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
            View.Template.Cur.VisibilityPredicate = prop;

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