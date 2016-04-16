using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoJsGridViewGen
    {
        void GenerateJSViewModelCore(WebViewGenContext context)
        {
            var view = context.View;

            // Extend base component view model.
            OB($"var ViewModel = (function (_super)");

            O("casimodo.__extends(ViewModel, _super);");

            O();
            OB("function ViewModel(options)");
            O("_super.call(this, options);");
            O($"this.keyName = \"{context.View.TypeConfig.Key.Name}\";");
            End();

            O();
            O("var fn = ViewModel.prototype;");

            // Define main event handler functions and call each specific function.
            foreach (var item in JsFuncs.ComponentEventHandlers.Where(x => x.IsContainer))
            {
                O();
                OB($"fn.{item.FunctionName} = function (e)");

                foreach (var func in item.BodyFunctions)
                {
                    if (func.Call != null)
                    {
                        O(func.Call);
                    }
                    else if (func.FunctionName != null)
                    {
                        if (func.IsModelPart)
                            O($"this.{func.FunctionName}(e);");
                        else
                            O($"{func.FunctionName}(e);");
                    }
                }

                O($"this.trigger('{item.Kind.ToString().FirstLetterToLower()}', e);");
                if (item.Kind == KendoGridEvent.Changed)
                    O("this.onCurrentItemChanged();");

                End();
            }

            // View model functions.
            foreach (var func in JsFuncs.Functions.Where(x => x.IsModelPart && x.Body != null))
            {
                O();
                OB($"fn.{func.FunctionName} = function (e)");
                func.Body(context);
                End();
            }

            // Data model factory (used by the Kendo data source).
            O();
            OB("fn.createDataModel = function ()");
            OB("var model =");
            KendoGen.GenerateDataSourceModel(context, TransportConfig.ModelProps);
            End(";");
            // Add custom (computed) properties to the model.
            O();
            O("this.extendDataModel(model);");

            O();
            O("return model;");
            End(); // Data model factory.            

            // Data source factory.
            O();
            OB("fn.createDataSource = function ()");
            O("return this.dataSource ? this.dataSource : (this.dataSource = new kendo.data.DataSource(this.createDataSourceOptions()));");
            End();

            // Request url factory.
            O();
            OB("fn.createRequestUrl = function ()");
            O($"var url = \"{TransportConfig.ODataSelectUrl}\";");
            O("if (this.requestUrlFactory) return this.requestUrlFactory(url); else return url;");
            End();

            O();
            OB("fn.setRequestUrlFactory = function (factory)");
            O($"this.requestUrlFactory = factory;");
            End();

            // Data source options factory.
            O();
            OB("fn.createDataSourceOptions = function ()");
            O("if (this.dataSourceOptions) return this.dataSourceOptions;");
            OB("this.dataSourceOptions =");
            GenerateDataSourceOptions(context);
            End();
            // Set initial filters.
            O("if (this.filters.length)");
            O("    this.dataSourceOptions.filter = { filters: this.filters };");
            O("return this.dataSourceOptions;");
            End(); // Data source options factory.

            O();
            O("return ViewModel;");

            End(")(kendomodo.GridViewModelBase)"); // ViewModel class.

            O();
            OB("space.vm = new ViewModel(");
            // Constructor options
            O("space: space,");
            if (view.ItemSelection.IsMultiselect && view.ItemSelection.UseCheckBox)
                O("selectionMode: 'multiple'");
            End(");");
        }

        void GenerateJSViewModel(WebViewGenContext context)
        {
            var vm = context.ComponentViewModelName;
            var view = context.View;

            // Global data source accessor.
            O();
            OB($"space.getDataSource = function ()");
            O("return space.createViewModel().createDataSource();");
            End(";");

            // View model factory function.
            O();
            OB($"space.createViewModel = function ()");
            O("if (space.vm) return space.vm;");
            O();
            GenerateJSViewModelCore(context);

            // KABU TODO: REVISIT: This does not work.
            //// Add property data sources.
            //if (Options.IsFilterOverCurrentDataEnabled)
            //{
            //    foreach (var vprop in View.Props.Where(x => !x.FormedNavigationTo.Is))
            //    {
            //        OB($"self.addDataSourceForProp('{vprop.Name}', new kendo.data.DataSource(");
            //        O("type: 'odata-v4',");
            //        // Data transport
            //        OB("transport:");
            //        O("parameterMap: kendomodo.parameterMapForOData,");
            //        // Read                
            //        O($"read: {{ url: '{DataSource.ODataReadBaseUrl}$select={vprop.Name}' }},");
            //        End(); // transport
            //        End("));");
            //    }
            //}

            O();
            O("space.vm.init();");

            O();
            O("return space.vm;");

            End(); // ViewModel factory function.            

            // Non-view-model functions.
            var funcs = JsFuncs.Functions.Where(x => !x.IsModelPart && x.Body != null).ToArray();
            if (funcs.Any())
            {
                O();
                foreach (var func in funcs)
                {
                    OB($"function {func.FunctionName} (e)");
                    func.Body(context);
                    End();
                }
            }
        }

        protected void InitEvents(WebViewGenContext context)
        {
            JsFuncs.Component = context.ComponentName;
            JsFuncs.ViewModel = context.ComponentViewModelName;

            JsFuncs.Add(KendoGridEvent.DataBound).Call = "kendomodo.onGridViewModelDataBound(this, e);";
            JsFuncs.Add(KendoGridEvent.Changed).Call = "kendomodo.onGridViewModelChanged(this, e);";

            if (EditorView != null)
            {
                JsFuncs.Add(KendoGridEvent.Editing, "Default").Body = GenVM_JS_OnEditing_Default;
                JsFuncs.Add(KendoGridEvent.Editing, "Hide").Body = GenVM_JS_OnEditing_Hide;
                JsFuncs.Add(KendoGridEvent.Editing, "OnPropChanged").Body = GenVM_JS_OnEditing_OnPropChanged;
                JsFuncs.Add(KendoGridEvent.Editing, "QueryReferencedObject").Body = GenVM_JS_OnEditing_QueryReferencedObject;
                JsFuncs.Add(KendoGridEvent.Editing, "ExtendEditModel").Body = GenVM_JS_OnEditing_ExtendEditModel;
            }
            else
            {
                JsFuncs.RemoveComponentEventHandler(KendoGridEvent.Editing);
            }

            if (InlineDetailsView != null)
            {
                JsFuncs.Add(KendoGridEvent.DetailExpanding).Call = "kendomodo.onGridViewModelDetailExpanding(this, e);";
                JsFuncs.Add(KendoGridEvent.DetailCollapsing).Call = "kendomodo.onGridViewModelDetailCollapsing(this, e);";
            }
            else
            {
                JsFuncs.RemoveComponentEventHandler(KendoGridEvent.DetailInit);
                JsFuncs.RemoveComponentEventHandler(KendoGridEvent.DetailExpanding);
                JsFuncs.RemoveComponentEventHandler(KendoGridEvent.DetailCollapsing);
            }
        }

        protected void GenVM_JS_OnEditing(WebViewGenContext context)
        {
            // KABU TODO: REVISIT: Currently this is all not needed anymore/yet.
            return;

#pragma warning disable CS0162
            O("// Assign the edited data item to ViewModel.editItem.");
            O($"{context.ComponentViewModelName}.set('editItem', e.model);");

            //var editorView = $("div.k-edit-form-container");
            //editorView.each(function() {
            //    alert("Editor window found dynamic in global");
            //    kendo.bind($(this), grid_Contract_MainViewModel);
            //    return false;
            //});
#pragma warning restore CS0162
        }

        protected void GenVM_JS_OnEditing_ExtendEditModel(WebViewGenContext context)
        {
            var view = EditorView;
            var type = EditorView.TypeConfig;

            var vprops = view.Props.Where(x =>
                x.IsSelector &&
                // IMPORTANT: Operate on store props.
                x.StoreOrSelf.DbAnno.Sequence.Is)
                .ToArray();

            if (!vprops.Any())
                return;

            O($"var item = e.model;");

            foreach (var vprop in vprops)
            {
                // IMPORTANT: Operate on store props.
                var sprop = vprop.StoreOrSelf;

                OB($"item.set('is{vprop.Name}SelectorEnabled', function ()");

                var expression = sprop.DbAnno.Unique.GetParams()
                    .Select(per => $"this.get('{per.Prop.Name}') != null")
                    .Join(" && ");

                O($" return {expression};");

                End(");");
            }
        }


        protected void GenVM_JS_OnEditing_Default(WebViewGenContext context)
        {
            // Call the default kendomodo grid function.

            var options = "";
            if (CanDelete)
            {
                options = ", { canDelete: true && this.auth.canDelete }";
            }

            O($"kendomodo.onGridViewModelEditing(this, e{options});");
        }

        protected void GenVM_JS_OnEditing_Hide(WebViewGenContext context)
        {
            var hideProps = EditorView.Props.Where(x =>
                x.HideModes != MojViewMode.None &&
                x.HideModes != MojViewMode.All)
                .ToArray();

            var props = hideProps.Where(x => x.HideModes.HasFlag(MojViewMode.Create)).ToArray();
            if (props.Any())
            {
                OB("if (e.model.isNew())");
                GenVM_JS_HideProps(props);
                End();
            }

            props = hideProps.Where(x => x.HideModes.HasFlag(MojViewMode.Update)).ToArray();
            if (props.Any())
            {
                OB("if (!e.model.isNew())");
                GenVM_JS_HideProps(props);
                End();
            }
        }

        protected void GenVM_JS_HideProps(IEnumerable<MojViewProp> props)
        {
            foreach (var prop in props)
            {
                string marker = prop.GetHideModesMarker();
                O($"$('div.form-group.{marker}').remove();");
            }
        }

        IEnumerable<MiaPropSetterConfig> GetQueryPropSettersOfOnCreateTriggers(MojType queriedForeignType)
        {
            var type = View.TypeConfig.RequiredStore;
            queriedForeignType = queriedForeignType.RequiredStore;

            if (CanCreate)
            {
                // Extend the query by props which need to be assigned from a parent
                // entity to this entity when creating a new entity.

                // KABU TODO: This is for edit-create-mode only, i.e. not for edit-modify-mode.
                //   We need to know here in which mode we operate.

                foreach (var setter in queriedForeignType.Triggers
                    .Where(x =>
                        x.Event == MiaTypeTriggerEventKind.Create &&
                        x.CrudOp == MojCrudOp.Create &&
                        x.TargetType == type)
                    .SelectMany(x => x.Operations.Items)
                    .OfType<MiaPropSetterConfig>())

                    yield return setter;
            }
        }

        IEnumerable<MojDataGraphNode> GetQueryNodesOfPropChangedTriggers(MojType queriedForeignType)
        {
            var type = View.TypeConfig.RequiredStore;
            queriedForeignType = queriedForeignType.RequiredStore;

            // Select prop changed triggers which have value where the foreign type is used.
            foreach (var setter in type.Triggers
                .Where(x => x.Event == MiaTypeTriggerEventKind.PropChanged)
                .SelectMany(x => x.Operations.Items)
                .OfType<MiaPropSetterConfig>()
                .Where(x => !x.IsNativeSource))
            {
                int stepIndex = setter.Source.FormedNavigationTo.StepIndexOfTarget(queriedForeignType);
                if (stepIndex != -1)
                {
                    QueriedTriggerPropSetters.Add(setter);

                    var node = setter.Source.FormedNavigationTo.BuildDataGraph(startDepth: stepIndex + 1);

                    yield return node;
                }
            }
        }

        public List<MiaPropSetterConfig> QueriedTriggerPropSetters { get; set; } = new List<MiaPropSetterConfig>();

        protected void GenVM_JS_OnEditing_OnPropChanged(WebViewGenContext context)
        {
            var type = EditorView.TypeConfig.RequiredStore;

            var triggers = type.Triggers.Where(x => x.Event == MiaTypeTriggerEventKind.PropChanged).ToArray();
            var cascadeTargetProps = type.GetProps().Where(x => x.CascadeFromProps.Any()).ToArray();

            if (!triggers.Any() && !cascadeTargetProps.Any())
                return;

            O("var item = e.model;");

            OBegin("item.bind('change', function (e)");

            if (triggers.Any())
                GenVM_JS_OnEditing_OnPropChanged_TriggersCore(context, triggers);

            if (cascadeTargetProps.Any())
                GenVM_JS_OnEditing_OnPropChanged_Cascades(context, cascadeTargetProps);

            End(");");
        }

        protected void GenVM_JS_OnEditing_OnPropChanged_Cascades(WebViewGenContext context, MojProp[] cascadeTargetProps)
        {
            foreach (var cascadeTargetProp in cascadeTargetProps)
            {
                var viewCascadeTargetProp = EditorView.Props.FirstOrDefault(x => x.Id == cascadeTargetProp.Id);
                if (viewCascadeTargetProp == null)
                {
                    if (viewCascadeTargetProp == null)
                        throw new MojenException($"Cascade target property '{cascadeTargetProp.Name}' is missing in the editor view.");
                }

                // If any of the cascade-from props has changed...
                Oo($"if (");

                int i = 0;
                foreach (var cascadeFromProp in cascadeTargetProp.CascadeFromProps)
                {
                    var viewCascadeFromProp = EditorView.Props.FirstOrDefault(x => x.Id == cascadeTargetProp.Id);
                    if (viewCascadeFromProp == null)
                        throw new MojenException($"Cascade-from property '{cascadeTargetProp.Name}' is missing in the editor view.");

                    o($"e.field === '{cascadeFromProp.Name}'");

                    if (++i < cascadeTargetProp.CascadeFromProps.Count)
                        o(" || ");
                }

                oO(") {");
                Push();

                // Set cascade target prop to NULL.
                O($"item.set('{cascadeTargetProp.Name}', null);");

                // If applicable then also set the foreign key to null.
                // Otherwise the referenced entity will not be fully cleared out.
                if (cascadeTargetProp.Reference.Is &&
                    cascadeTargetProp.Reference.IsNavigation)
                {
                    O($"item.set('{cascadeTargetProp.Reference.ForeignKey.Name}', null);");
                }

                Pop();
                O("}");
            }
        }

        protected void GenVM_JS_OnEditing_OnPropChanged_TriggersCore(WebViewGenContext context, MiaTypeTriggerConfig[] triggers)
        {
            if (!triggers.Any())
                return;

            foreach (var trigger in triggers)
            {
                OB($"if (e.field === '{trigger.ContextProp.FormedTargetPath}')");
                foreach (var map in trigger.Operations.Items.OfType<MiaPropSetterConfig>())
                {
                    O($"item.set('{map.Target.FormedTargetPath}', item.get('{map.Source.FormedTargetPath}') || null);");
                }
                End();
            }
        }

        protected void GenVM_JS_OnEditing_QueryReferencedObject(WebViewGenContext context)
        {
            if (EditorView == null)
                return;

            var type = View.TypeConfig;

            var looseReferenceGroups =
                (from prop in EditorView.Props
                     // Select all read-only props with *loose* references.
                 where !prop.IsEditable
                 let step = prop.FormedNavigationTo.FirstLooseStep
                 where step != null
                 select new
                 {
                     Prop = prop,
                     Step = step,
                     ForeignKeyPath = step.SourceProp.GetFormedForeignKeyPath(),
                     ObjectPath = step.SourceProp.FormedTargetPath,
                     ObjectType = step.SourceProp.Reference.ToType,
                     ObjectPluralName = step.SourceProp.Reference.ToType.PluralName
                 } into item
                 // Group by foreign key path.
                 group item by item.ForeignKeyPath)
                .ToArray();

            if (!looseReferenceGroups.Any())
                return;

            O("var item = e.model;");

            OB("item.bind('change', function (e)");
            foreach (var looseReferenceGroup in looseReferenceGroups)
            {
                var reference = looseReferenceGroup.First();

                // KABU TODO: Will we actually get paths here or only prop names?
                OB($"if (e.field === '{reference.ForeignKeyPath}')");

                O($"item.set('{reference.ObjectPath}', null);");

                O($"var foreignKeyValue = item.get('{reference.ForeignKeyPath}');");

                OB($"if (foreignKeyValue)");

                // Build OData $select and $expand expressions
                var paths = looseReferenceGroup.Select(x => x.Prop.FormedNavigationTo).ToArray();

                var queryNodes = paths
                    .BuildDataGraph(
                        includeKey: true,
                        includeForeignKey: true,
                        startDepth: reference.Prop.FormedNavigationTo.Steps.Count);

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                var onCreateTriggerPropSetters = GetQueryPropSettersOfOnCreateTriggers(reference.ObjectType).ToArray();
                if (onCreateTriggerPropSetters.Any())
                {
                    // Merge
                    queryNodes = queryNodes.Merge(onCreateTriggerPropSetters.Select(x => x.Source).BuildDataGraph()).ToList();
                }

                var onPropChangedTriggerGraphNodes = GetQueryNodesOfPropChangedTriggers(reference.ObjectType).ToArray();
                if (onPropChangedTriggerGraphNodes.Any())
                {
                    // Merge
                    queryNodes = queryNodes.Merge(onPropChangedTriggerGraphNodes).ToList();
                }

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                // Build the OData $select and $expand expression.
                string expression = this.BuildODataSelectAndExpand(queryNodes);
                var odata = App.Get<WebODataBuildConfig>();
                KendoGen.ODataQuery($"'{odata.Path}/{reference.ObjectPluralName}/{this.GetODataQueryFunc()}()?{expression}&$filter=Id eq ' + foreignKeyValue", null, () =>
                {
                    // If result is available...
                    OBegin($"if (result && result.length)");

                    O($"item.set('{reference.ObjectPath}', result[0]);");

                    // Process on create triggers.
                    if (onCreateTriggerPropSetters.Any())
                    {
                        O();
                        O("var parent = result[0];");

                        // Assign properties.
                        foreach (var setter in onCreateTriggerPropSetters)
                        {
                            O($"item.set('{setter.Target.FormedTargetPath}', parent.{setter.Source.FormedTargetPath});");
                        }
                    }

                    // KABU TODO: REVISIT: Do we really want to have to clear all the fields if no entity is returned from the server?
                    //   The foreign key prop must be required in such cases anyway.
                    // KABU TODO: REMOVE?
                    // KABU TODO: IMPORTANT: Check that the foreign-key prop is required.

                    End(); // End of result available
#if (false)
                if (createOp != null)
                {
                    OB("else");
                    O("// Clear properties from parent.");
                    foreach (var mapping in createOp.Map.Mappings)
                    {
                        // KABU TODO: Won't work for value types.
                        O($"item.set('{mapping.Target.Name}', null);");
                    }
                    End();
                }
#endif
                });

                End(); // Property not null

                End(); // Property name matches
            }
            End(");"); // Change handler

            // Example:
            // var item = e.model;
            // item.bind('change', function(e) {
            //    if (e.field === 'CustomerId') {
            //        item.Customer = null;
            //        if (item.CustomerId) {
            //            kendomodo.query('odata/Customers/Ga.Query()?$select=Id,Name2,Number&$filter=Id eq ' + item.CustomerId, null, function(result) {
            //                if (result && result.length)
            //                    item.set('Customer', result[0]);
            //            });
            //        }
            //    }
            // });
        }

        protected KendoGridLookupColInfo GetLookupColInfo(MojViewProp prop)
        {
            var info = new KendoGridLookupColInfo();
            var navi = prop.FormedNavigationTo;
            info.Identifier = navi.TargetPath.Replace(".", "");
            info.LookupFunction = "lookup" + info.Identifier;
            info.ODataUrl = this.GetODataLookupUrl(navi);
            info.ValueProp = navi.TargetType.Key;
            info.DisplayProp = navi.TargetProp;

            return info;
        }

        protected class KendoGridLookupColInfo
        {
            public string Identifier { get; set; }
            public string LookupFunction { get; set; }
            public MojProp ValueProp { get; set; }
            public MojProp DisplayProp { get; set; }
            public string ODataUrl { get; set; }
        }

        protected string GetODataLookupUrl(MojFormedNavigationPath path)
        {
            var targetType = path.TargetType;
            var targetProp = path.TargetProp;
            var url =
                this.GetODataPath(targetType) +
                $"/{this.GetODataQueryFunc()}()?$select={targetType.Key.Name},{targetProp.Name}";

            return url;
        }
    }
}