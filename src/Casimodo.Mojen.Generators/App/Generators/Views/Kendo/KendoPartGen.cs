﻿namespace Casimodo.Mojen
{
    public partial class KendoPartGen : WebPartGenerator
    {
        public void OEditableFormComponent(WebViewGenContext context)
        {
            // Standalone editable form component.
            var view = context.View;
            var transport = this.CreateODataTransport(view, editorView: view);

            OTsNamespace(WebConfig.ScriptUINamespace, (nscontext) =>
            {
                OBeginComponentFactory(context);

                OB("return new kmodo.EditorForm(");
                OViewModelOptions(context,
                    extend: () =>
                    {
                        if (!transport.IsLocalData)
                        {
                            // OData read query URL
                            O($"readQuery: {Moj.JS(transport.ODataSelectUrl)},");

                            OB("transport: e =>");
                            OB("return");
                            ODataSourceTransportOptions(context, GetDataSourceSingleConfig(context, transport,
                                create: context.View.CanCreate,
                                modify: context.View.CanModify,
                                delete: context.View.CanDelete));
                            End(";");
                            End(",");
                        }

                        OB("dataModel: e =>");
                        OB("return");
                        ODataSourceModelOptions(context, transport.ModelProps);
                        End(";");
                        End(",");

                        OViewModelOnEditingOption(context.View, context.View.CanCreate);
                    });
                End(").init();");

                OEndComponentFactory(context);
            });
        }

        public void OReadOnlyFormComponent(WebViewGenContext context)
        {
            // Standalone read-only form component.

            var view = context.View;
            var transport = this.CreateODataTransport(view);

            OTsNamespace(WebConfig.ScriptUINamespace, (nscontext) =>
            {
                OBeginComponentFactory(context);

                OB("return new kmodo.ReadOnlyForm(");
                OViewModelOptions(context,
                    extend: () =>
                    {
                        if (!transport.IsLocalData)
                        {
                            // OData read query URL
                            O($"readQuery: {Moj.JS(transport.ODataSelectUrl)},");

                            OB("transport: e =>");
                            OB("return");
                            ODataSourceTransportOptions(context, GetDataSourceSingleConfig(context, transport));
                            End(";");
                            End(",");
                        }

                        OB("dataModel: e =>");
                        OB("return");
                        ODataSourceModelOptions(context, transport.ModelProps);
                        End(";");
                        End(",");
                    });
                End(").init();");

                OEndComponentFactory(context);
            });
        }

        // KABU TODO: MAGIC: Move to config layer.
        public class GeoPlaceLookupWebViewConfig
        {
            public Guid Id { get; set; } = new Guid("c2383283-cb48-4ece-9066-667f5c623a95");
            public string Url { get; set; } = "/GoogleMap/Lookup";
            public string Title { get; set; } = "Adresse suchen";
            public int Width { get; set; } = 1000;
            public int Height { get; set; } = 700;
        }

        public void OOpenGeoPlaceLookupView(WebViewGenContext context, Action ok, object options = null)
        {
            var view = new GeoPlaceLookupWebViewConfig();

            Oo($"kmodo.openById('{view.Id}',");

            if (options is Action)
            {
                ob("");
                (options as Action)?.Invoke();
                Oeo(",");
            }
            else
                o($" {Moj.JS(options, quote: false)},");

            ob(" function (result)");
            OB("if (result.isOk)");
            ok();
            End();
            End(");");
        }

        public void OOpenDialogView(WebViewGenContext context, MojViewConfig dialogView, Action ok, object options = null)
        {
            Oo($"kmodo.openById('{dialogView.Id}',");

            if (options is Action)
            {
                ob("");
                (options as Action)?.Invoke();
                Oeo(",");
            }
            else
                o($" {Moj.JS(options, quote: false)},");

            ob(" function (result)");
            OB("if (result.isOk)");
            ok();
            End();
            End(");");
        }

        public void OBeginComponentFactory(WebViewGenContext context)
        {
            O($"export const {context.ViewModelFactoryName} = cmodo.createComponentFactory();");
            OB($"{context.ViewModelFactoryName}.createCore = function (options)");
        }

        public void OEndComponentFactory(WebViewGenContext context)
        {
            End();
        }

        public WebViewGenContext InitComponentNames(WebViewGenContext context)
        {
            context.UINamespace = GetJsScriptUINamespace(context.View);
            context.ComponentName = BuildJsClassName(context.View);
            context.ViewModelFactoryName = BuildJsClassName(context.View) + "Factory";
            context.ViewModelFactoryFullName = GetJsScriptUINamespace(context.View) + "." + context.ViewModelFactoryName;

            return context;
        }


        // KABU TODO: REMOVE? Not used anymore because we're accessing nested objects via
        //  kendo observable's accessor function now.
        void GetNotNullExpressionTemplate(StringBuilder sb, MojViewProp prop, bool checkLastProp = false)
        {
            // Example:
            // typeof Company !== 'undefined' && Company && Company.NameShort
            var steps = prop.FormedTargetPathNames.ToArray();
            var length = steps.Length - (checkLastProp ? 0 : 1);
            for (int i = 0; i < length; i++)
            {
                if (i == 0) sb.o($"typeof {steps[0]}!=='undefined'&&");

                for (int k = 0; k <= i; k++)
                {
                    sb.o($"{steps[k]}");

                    if (k < i) sb.o(".");
                }

                if (i < length - 1) sb.o("&&");
            }
        }

        // KABU TODO: REMOVE? Not used
        public void OWindowOptions(KendoWindowConfig window)
        {
            // See http://docs.telerik.com/KENDO-UI/api/javascript/ui/window

            var options = XBuilder.CreateAnonymous();

            XBuilder animation = null;
            if (window.Animation == null || (window.Animation as bool?) == true)
            {
                if (window.IsParentModal)
                    animation = options.Add("animation", "false");
                else
                    animation = options.Add("animation", "kmodo.getDefaultDialogWindowAnimation()");
            }

#if (false)
            if (window.Open.Is)
                animation.O("open")
                    .O("effects", window.Open.Effects).End()
                    .O("duration", window.Open.Duration).End();

            if (window.Close.Is)
                animation.O("close")
                    .O("effects", window.Close.Effects).End()
                    .O("duration", window.Close.Duration).End();
#endif
            if (window.ContentUrl != null)
            {
                options.Add("content")
                    .Add("url", window.ContentUrl, text: true);
            }

            options.Add("modal", window.IsModal);
            options.Add("visible", window.IsVisible);
            if (window.Title != null) options.Add("title", window.Title, text: true);

            var width = window.Width ?? window.MinWidth ?? window.MaxWidth;
            if (width != null) options.Add("width", width);

            if (window.MinWidth != null) options.Add("minWidth", window.MinWidth);
            if (window.MaxWidth != null) options.Add("maxWidth", window.MaxWidth);

            var height = window.Height ?? window.MinHeight;
            if (height != null) options.Add("height", height);

            if (window.MinHeight != null) options.Add("minHeight", window.MinHeight);

            if (window.OnClosing != null) options.Add("close", window.OnClosing);
            if (window.OnDeactivated != null) options.Add("deactivate", window.OnDeactivated);

            OJsObjectLiteral(options.Elem, trailingNewline: false, trailingComma: false);
        }

        public void OODataSourceReadOptions(WebViewGenContext context, string query)
        {
            ODataSourceOptions(context, new KendoDataSourceConfig
            {
                ReadQuery = query
            });
        }

        KendoDataSourceConfig GetDataSourceSingleConfig(WebViewGenContext context, MojHttpRequestConfig transport,
            bool create = false, bool modify = false, bool delete = false)
        {
            return new KendoDataSourceConfig
            {
                TypeConfig = context.View.TypeConfig,
                TransportType = "odata-v4",
                TransportConfig = transport,
                // NOTE: Grouped views will use OData *actions* for updates.
                UseODataActions = context.View.Group != null,
                DataModelFactory = "e.sender.createDataModel()",
                ReadQueryFactory = "e.sender.createReadQuery()",
                CanCreate = create,
                CanModify = modify,
                CanDelete = context.View.CanDelete,
                PageSize = 1
            };
        }


        public string BuildCoreFiltersArrayLiteral(WebViewGenContext context)
        {
            if (!context.View.HasFilters)
                return Moj.JS(null);

            var filters = new List<string>();
            var sb = new StringBuilder();

            if (context.View.IsFilteredByLoggedInPerson)
                sb.Append(
                    $"{{ field: '{context.View.FilteredByLoogedInPersonProp}', " +
                    "operator: 'eq', " +
                    "value: cmodo.run.authInfo.userId },");

            if (context.View.SimpleFilter != null)
                sb.Append((KendoDataSourceMex.ToKendoDataSourceFilters(context.View.SimpleFilter)));

            return "[" + sb.ToString() + "]";
        }

        public void ODataSourceListOptions(WebViewGenContext context, MojHttpRequestConfig transport,
            bool create, bool modify, bool delete,
            int pageSize, bool isServerPaging, MojViewProp[] initialSortProps)
        {
            ODataSourceOptions(context, new KendoDataSourceConfig
            {
                TypeConfig = context.View.TypeConfig,
                TransportType = "odata-v4",
                TransportConfig = transport,
                // NOTE: Grouped views will use OData *actions* for updates.
                UseODataActions = context.View.Group != null,

                // The data-source's model is created by the view model.
                DataModelFactory = "e.sender.createDataModel()",
                ReadQueryFactory = "e.sender.createReadQuery()",

                CanCreate = create,
                CanModify = modify,
                CanDelete = delete,

                PageSize = pageSize,
                IsServerPaging = isServerPaging,
                InitialSortProps = initialSortProps,
            });
        }

        public void OViewModelOptions(WebViewGenContext context, string title = null,
            bool isList = false, bool dataType = true,
            Action extend = null)
        {
            var view = context.View;

            title ??= context.View.Title;
            if (string.IsNullOrWhiteSpace(title))
                title = isList ? view.TypeConfig.DisplayPluralName : view.TypeConfig.DisplayName;

            O($"title: {Moj.JS(title)},");
            O($"id: {Moj.JS(view.Id)},");
            O($"part: {Moj.JS(view.TypeConfig.Name)},");
            O($"group: {Moj.JS(view.Group)},");
            O($"role: {Moj.JS(view.MainRoleName)},");
            if (dataType)
            {
                O($"dataTypeName: {Moj.JS(view.TypeConfig.Name)},");
                O($"dataTypeId: {Moj.JS(view.TypeConfig.Id)},");
            }
            O($"isLookup: {Moj.JS(view.Lookup.Is)},");
            O($"isDialog: {Moj.JS(view.IsDialog)},");
            O($"isAuthRequired: {Moj.JS(view.IsAuthEnabled)},");

            if (view.IsLocalData)
                O("isLocalData: true,");

            O($"isCustomSave: {Moj.JS(view.IsCustomSave)},");

            // Company filters
            O($"isCompanyFilterEnabled: {Moj.JS(view.IsCompanyFilterEnabled)},");
            O($"isGlobalCompanyFilterEnabled: {Moj.JS(view.IsGlobalCompanyFilterEnabled == true)},");

            if (isList)
            {
                // Selection
                if (view.ItemSelection.IsMultiselect && view.ItemSelection.UseCheckBox)
                    O("selectionMode: 'multiple',");

                // TODO: REMOVE: O("hasRowContextMenu: {0},", Moj.JS(context.View.HasListItemContextMenu));
                // Tags
                O($"isTaggable: {Moj.JS(view.IsTaggable)},");
                O($"isTagsFilterEnabled: {Moj.JS(view.IsTagsFilterEnabled)},");
                O($"tagsEditorId: {Moj.JS(view.TagsEditorView?.Id)},");
            }

            // KABU TODO: REMOVE? OViewDimensionOptions(view);          

            O("extra: options || null,");

            if (view.EditorView != null)
            {
                OB("editor:");
                O($"id: {Moj.JS(view.EditorView.Id)},");
                O($"url: {Moj.JS(view.EditorView.Url, nullIfEmptyString: true)},");
                // OViewDimensionOptions(view.EditorView);
                End(",");
            }

            extend?.Invoke();
        }

        // KABU TODO: REMOVE?
        //void OViewDimensionOptions(MojViewConfig view)
        //{
        //    O("width: {0},", MojenUtils.ToJsValue(view.Width));
        //    O("minWidth: {0},", MojenUtils.ToJsValue(view.MinWidth));
        //    O("maxWidth: {0},", MojenUtils.ToJsValue(view.MaxWidth));
        //    O("height: {0},", MojenUtils.ToJsValue(view.Height));
        //    O("minHeight: {0},", MojenUtils.ToJsValue(view.MinHeight));
        //    O("maxHeight: {0},", MojenUtils.ToJsValue(view.MaxHeight));
        //    O("maximize: {0},", MojenUtils.ToJsValue(view.IsMaximized));
        //}

        public void BindPageContentView(MojControllerViewConfig view, MojViewRole role)
        {
            var controller = view.Controller;

            foreach (var contentView in App.GetItems<MojControllerViewConfig>()
                .Where(x =>
                    x.Controller == view.Controller &&
                    x != view &&
                    x.Group == view.Group &&
                    x.Kind.Roles.HasFlag(role)))
            {
                if (!view.ContentViews.Contains(contentView))
                    view.ContentViews.Add(contentView);
            }
        }

        public void BindEditorView<TEditorGen>(MojViewConfig view)
            where TEditorGen : KendoTypeViewGenBase
        {
            if (view.EditorView != null || view.IsEditor || view.IsLookup || !(view is MojControllerViewConfig))
                return;

            var controller = (view as MojControllerViewConfig).Controller;

            // Try to find a matching editor.
            view.EditorView = App.GetItems<MojControllerViewConfig>()
                .Where(x =>
                    x != view &&
                    x.Controller == controller &&
                    x.Group == view.Group &&
                    x.Uses<TEditorGen>() &&
                    x.CanModify)
                .SingleOrDefault();

            if (view.EditorView != null)
            {
                new MojViewBuilder(view).EnsureEditAuthControlPropsIfMissing();
            }
        }

        public void BindCustomTagsEditorView(MojViewConfig view)
        {
            var controller = (view as MojControllerViewConfig).Controller;

            // Try to find a matching editor.
            view.TagsEditorView = App.GetItems<MojControllerViewConfig>()
                .Where(x =>
                    x != view &&
                    x.Controller == controller &&
                    x.Group == "Tags" &&
                    x.Uses<HardCodedKendoTagsEditorViewGen>())
                .SingleOrDefault();
        }
    }
}
