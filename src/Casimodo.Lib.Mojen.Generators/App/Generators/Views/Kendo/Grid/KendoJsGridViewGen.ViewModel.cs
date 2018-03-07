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

            KendoGen.OJsViewModelClass("ViewModel", extends: "kendomodo.ui.GridViewModel",
            construct: () =>
            {
                O($"this.keyName = \"{context.View.TypeConfig.Key.Name}\";");
                if (HasViewModelExtension)
                    O($"this.extension = new {DataConfig.ScriptUINamespace}.{ViewModelExtensionClassName}({{ vm: this }});");
            },
            content: () =>
            {
                // Request url factory.
                OB("fn.createRequestUrl = function ()");
                O($"var url = \"{TransportConfig.ODataSelectUrl}\";");
                O("if (this.requestUrlFactory) return this.requestUrlFactory(url); else return url;");
                End(";"); // Request url factory.

                // Data model factory (used by the Kendo data source).
                O();
                OB("fn.createDataModel = function ()");
                OB("var model =");
                KendoGen.GenerateDataSourceModel(TransportConfig.ModelProps);
                End(";");
                // Add custom (computed) properties to the model.
                O();
                O("this.extendDataModel(model);");
                O();
                O("return model;");
                End(";"); // Data model factory.              

                // Filters
                if (View.HasFilters)
                {
                    O();
                    OB("fn.getBaseFilters = function ()");
                    O("var filters = [];");

                    if (View.IsFilteredByLoggedInPerson)
                    {
                        O($"filters.push({{ field: '{View.FilteredByLoogedInPersonProp}', " +
                            $"operator: 'eq', value: window.casimodo.run.authInfo.UserId }});");
                    }

                    if (View.SimpleFilter != null)
                    {
                        O($"filters.push.apply(filters, {KendoDataSourceMex.ToKendoDataSourceFilters(View.SimpleFilter)});");
                    }

                    O("return filters;");
                    End(";");
                }

                // Data source options factory.
                O();
                KendoGen.ODataSourceOptionsFactory(context, () => GenerateODataV4DataSourceOptions(context));
                // KABU TODO: REMOVE
                //OB("fn.createDataSourceOptions = function ()");
                //O("if (this.dataSourceOptions) return this.dataSourceOptions;");
                //OB("this.dataSourceOptions =");
                //GenerateODataV4DataSourceOptions(context);
                //End(";");
                //// Set initial filters.
                //O("if (this.filters.length)");
                //O("    this.dataSourceOptions.filter = { filters: this.filters };");
                //O("return this.dataSourceOptions;");
                //End(";"); // Data source options factory.

                // Define main event handler functions and call each specific function.
                foreach (var item in JsFuncs.ComponentEventHandlers.Where(x => x.IsContainer && !x.IsExistent))
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

                    // Re-trigger the widget's event using the widget's name for the event.
                    O($"this.trigger('{item.Event.ToString().FirstLetterToLower()}', e);");

                    if (item.Event == KendoGridEvent.Changed)
                        // Call view model's current item changed handler.
                        O("this.onCurrentItemChanged();");

                    End(";");
                }

                // View model functions.
                foreach (var func in JsFuncs.Functions.Where(x => x.IsModelPart && x.Body != null))
                {
                    O();
                    OB($"fn.{func.FunctionName} = function (e)");
                    func.Body(context);
                    End(";");
                }
            });

            // Create view model with options.
            O();
            OB("space.vm = new ViewModel(");
            O("space: space,");
            KendoGen.OJsViewModelConstructorOptions(context, isList: true);
            End(");");
        }

        void GenerateJSViewModel(WebViewGenContext context)
        {
            var vm = context.ComponentViewModelName;
            var view = context.View;

            // View model factory function.
            O();
            OB($"space.createViewModel = function ()");
            O("if (space.vm) return space.vm;");
            O();
            GenerateJSViewModelCore(context);

            O();
            O("space.vm.init();");

            O();
            O("return space.vm;");

            End(";"); // ViewModel factory function.                        
        }

        protected void InitEvents(WebViewGenContext context)
        {
            JsFuncs.ComponentName = context.ComponentName;
            JsFuncs.ViewModel = context.ComponentViewModelName;

            if (EditorView != null)
            {
                JsFuncs.Add(KendoGridEvent.Editing).Call = "this.onComponentEditingBase(e);";
                JsFuncs.Add(KendoGridEvent.Editing, "Hide").Body = GenVM_JS_OnEditing_Hide;
                JsFuncs.Add(KendoGridEvent.Editing, "OnPropChanged").Body = GenVM_JS_OnEditing_OnPropChanged;
                JsFuncs.Add(KendoGridEvent.Editing, "QueryReferencedObject").Body = GenVM_JS_OnEditing_QueryReferencedObject;
                JsFuncs.Add(KendoGridEvent.Editing, "ExtendEditModel").Body = GenVM_JS_OnEditing_ExtendEditModel;
            }
            else
            {
                JsFuncs.RemoveComponentEventHandler(KendoGridEvent.Editing);
                JsFuncs.RemoveComponentEventHandler(KendoGridEvent.BeforeEditing);
            }

            if (InlineDetailsView == null)
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

        protected void GenVM_JS_OnEditing_Hide(WebViewGenContext context)
        {
            var hideProps = EditorView.Props.Where(x =>
                x.HideModes != MojViewMode.None &&
                x.HideModes != MojViewMode.All)
                .ToArray();

            // Hide properties on create.
            var props = hideProps.Where(x => x.HideModes.HasFlag(MojViewMode.Create)).ToArray();
            if (props.Any())
            {
                // Execute when the model *is* new.
                OB("if (e.model.isNew())");
                GenVM_JS_HideProps(props);
                End();
            }

            // Hide properties on update.
            props = hideProps.Where(x => x.HideModes.HasFlag(MojViewMode.Update)).ToArray();
            if (props.Any())
            {
                // Execute when the model is *not* new.
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

                // Build OData query.
                var odataQuery = $"{App.Get<WebODataBuildConfig>().Path}/{reference.ObjectPluralName}/{this.GetODataQueryFunc()}()?{expression}";

                // On create triggers.
                var sourceAssignments = new List<string>();
                // Assign properties.
                foreach (var setter in onCreateTriggerPropSetters)
                    sourceAssignments.Add($"{{ t: '{setter.Target.FormedTargetPath}', s: '{setter.Source.FormedTargetPath}'}}");

                O($"if (e.field === '{reference.ForeignKeyPath}') this._onEditingQueryReferencedObject(" +
                    $"item, " +
                    $"'{reference.ObjectPath}', " +
                    $"e.field, '{odataQuery}', " +
                    $"'{reference.ObjectType.Key.Name}', " +
                    $"[{sourceAssignments.Join(", ")}]);");

                // Example:
                // if (e.field === 'ContractId') this._onEditingQueryReferencedObject(
                //    item, 'Contract', e.field, 'Id',
                //    '/odata/Contracts/Ga.Query()?$select=Id,City,CountryStateId,CountryId,Street,ZipCode&$expand=CountryState($select=Id,DisplayName),Country($select=Id,DisplayName)',
                //    [{ t: 'Street', s: 'Street'}, { t: 'ZipCode', s: 'ZipCode'}]);

                // KABU TODO: REMOVE: Moved this all of this to a function on the JS grid view model.
#if (false)
                // KABU TODO: Will we actually get paths here or only prop names?
                OB($"if (e.field === '{reference.ForeignKeyPath}')");

                O($"item.set('{reference.ObjectPath}', null);");

                O($"var foreignKeyValue = item.get('{reference.ForeignKeyPath}');");

                OB($"if (foreignKeyValue)");

                KendoGen.ODataQueryFirstOrDefault($"'{odata.Path}/{reference.ObjectPluralName}/{this.GetODataQueryFunc()}()?{expression}&$filter=Id eq ' + foreignKeyValue", null, () =>
                {

                    O($"item.set('{reference.ObjectPath}', result[0]);");

                    // Process on create triggers.
                    if (onCreateTriggerPropSetters.Any())
                    {
                        O();
                        OB("if (item.isNew())");

                        O("var source = result[0];");

                        // Assign properties.
                        foreach (var setter in onCreateTriggerPropSetters)
                        {
                            O($"item.set('{setter.Target.FormedTargetPath}', source.{setter.Source.FormedTargetPath});");
                        }
                        End();
                    }
                });

                End(); // Property not null

                End(); // Property name matches
#endif
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