﻿using Casimodo.Lib.Data;
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
            constructor: () =>
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

                // Define main event handler functions and call each specific function.
                foreach (var item in JsFuncs.EventHandlers.Where(x => x.IsContainer && !x.IsExistent))
                {
                    O();
                    OB($"fn.{item.FunctionName} = function (e, context)");

                    if (item.Call != null)
                        O(item.Call);

                    item.Body?.Invoke(context);

                    foreach (var child in item.Children)
                    {
                        if (child.Call != null)
                            O(child.Call);

                        if (child.FunctionName != null)
                            O($"this.{child.FunctionName}(e);");
                    }

                    // KABU TODO: REMOVE?
                    // Re-trigger the widget's event using the widget's name for the event.
                    //O($"this.trigger('{item.Event.ToString().FirstLetterToLower()}', e);");

                    End(";");
                }

                // View model functions.
                foreach (var func in JsFuncs.Functions.Where(x => x.Body != null))
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
            if (view.EditorView != null)
            {
                OB("editor:");
                O("viewId: '{0}',", view.EditorView.Id);
                O("url: {0},", MojenUtils.ToJsValue(view.EditorView.Url, nullIfEmptyString: true));
                O("width: {0},", MojenUtils.ToJsValue(view.EditorView.Width));
                O("height: {0},", MojenUtils.ToJsValue(view.EditorView.MinHeight));
                End();
            }
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

        void InitEvents(WebViewGenContext context)
        {
            JsFuncs.ComponentName = context.ComponentName;
            JsFuncs.ViewModel = context.ComponentViewModelName;

            if (EditorView != null)
            {
                JsFuncs.Use(KendoGridEvent.Editing, "edit").Body = GenOnEditing;
            }
        }

        void GenOnEditing(WebViewGenContext context)
        {
            O("var self = this;");
            O("var item = e.model;");
            O("var isNew = e.model.isNew();");

            GenOnEditing_Hide(context);
            GenOnEditing_OnPropChanged(context);
            GenOnEditing_ExtendEditModel(context);
            GenOnEditing_QueryReferencedObject(context);
        }

        void GenOnEditing_Hide(WebViewGenContext context)
        {
            O();

            var hideProps = EditorView.Props.Where(x =>
                x.HideModes != MojViewMode.None &&
                x.HideModes != MojViewMode.All)
                .ToArray();

            // Hide properties on create.
            var props = hideProps.Where(x => x.HideModes.HasFlag(MojViewMode.Create)).ToArray();
            if (props.Any())
            {
                // Execute when the model *is* new.
                OB("if (isNew)");
                GenOnEditing_HideProps(props);
                End();
            }

            // Hide properties on update.
            props = hideProps.Where(x => x.HideModes.HasFlag(MojViewMode.Update)).ToArray();
            if (props.Any())
            {
                // Execute when the model is *not* new.
                OB("if (!isNew)");
                GenOnEditing_HideProps(props);
                End();
            }
        }

        void GenOnEditing_HideProps(IEnumerable<MojViewProp> props)
        {
            foreach (var prop in props)
            {
                string marker = prop.GetHideModesMarker();
                O($"context.$view.find('div.form-group.{marker}').remove();");
            }
        }

        public List<MiaPropSetterConfig> QueriedTriggerPropSetters { get; set; } = new List<MiaPropSetterConfig>();

        void GenOnEditing_OnPropChanged(WebViewGenContext context)
        {
            var type = EditorView.TypeConfig.RequiredStore;

            var triggers = type.Triggers.Where(x => x.Event == MiaTypeTriggerEventKind.PropChanged).ToArray();
            var cascadeTargetProps = type.GetProps().Where(x => x.CascadeFromProps.Any()).ToArray();

            if (!triggers.Any() && !cascadeTargetProps.Any())
                return;

            O();

            OBegin("e.model.bind('change', function (e)");

            if (triggers.Any())
                GenOnEditing_OnPropChanged_TriggersCore(context, triggers);

            if (cascadeTargetProps.Any())
                GenOnEditing_OnPropChanged_Cascades(context, cascadeTargetProps);

            End(");");
        }

        void GenOnEditing_OnPropChanged_Cascades(WebViewGenContext context, MojProp[] cascadeTargetProps)
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

        void GenOnEditing_OnPropChanged_TriggersCore(WebViewGenContext context, MiaTypeTriggerConfig[] triggers)
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

        void GenOnEditing_QueryReferencedObject(WebViewGenContext context)
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

            O();

            OB("e.model.bind('change', function (e)");
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

                O($"if (e.field === '{reference.ForeignKeyPath}') self._onEditingQueryReferencedObject(" +
                    $"item, " +
                    $"'{reference.ObjectPath}', " +
                    $"'{reference.ForeignKeyPath}', " +
                    $"'{reference.ObjectType.Key.Name}', " +
                    $"'{odataQuery}', " +
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
        }

        void GenOnEditing_ExtendEditModel(WebViewGenContext context)
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

            O();

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

        KendoGridLookupColInfo GetLookupColInfo(MojViewProp prop)
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

        class KendoGridLookupColInfo
        {
            public string Identifier { get; set; }
            public string LookupFunction { get; set; }
            public MojProp ValueProp { get; set; }
            public MojProp DisplayProp { get; set; }
            public string ODataUrl { get; set; }
        }

        string GetODataLookupUrl(MojFormedNavigationPath path)
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