using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    public partial class KendoPartGen : WebPartGenerator
    {
        public void OViewModelOnEditingOption(MojViewConfig view, bool canCreate)
        {
            OB("editing: e =>");

            GenOnEditing_OnPropChanged(view);
            GenOnEditing_ExtendEditModel(view);
            GenOnEditing_QueryReferencedObject(view, canCreate);
            GenOnEditing_SetLoggedInPerson(view);

            End(",");
        }

        void GenOnEditing_SetLoggedInPerson(MojViewConfig view)
        {
            var loggedInPersonProps = view.Props.Where(x => x.IsLoggedInPerson).ToArray();
            if (!loggedInPersonProps.Any())
                return;

            O();
            OB("if (e.isNew)");
            foreach (var prop in loggedInPersonProps)
                O($"e.item.set('{prop.Reference.ForeignKey.Name}', cmodo.run.authInfo.userId);");
            End();
        }

        public List<MiaPropSetterConfig> QueriedTriggerPropSetters { get; set; } = [];

        void GenOnEditing_OnPropChanged(MojViewConfig view)
        {
            var type = view.TypeConfig.GetUnderlyingDataType();

            var triggers = type.Triggers.Where(x => x.Event == MiaTypeTriggerEventKind.PropChanged).ToArray();
            var cascadeTargetProps = type.GetProps()
                // Get all cascade target props from the MojType.
                .Where(x => x.CascadeFromProps.Any())
                // Use only what's in the view.
                .Where(x => view.Props.Any(y => y.Id == x.Id))
                .ToArray();

            if (!triggers.Any() && !cascadeTargetProps.Any())
                return;

            O();

            OBegin("e.item.bind('change', function (ce)");

            if (triggers.Any())
                GenOnEditing_OnPropChanged_TriggersCore(triggers);

            if (cascadeTargetProps.Any())
                GenOnEditing_OnPropChanged_Cascades(view, cascadeTargetProps);

            End(");");
        }

        void GenOnEditing_OnPropChanged_Cascades(MojViewConfig view, MojProp[] cascadeTargetProps)
        {
            foreach (var cascadeTargetProp in cascadeTargetProps)
            {
                var viewCascadeTargetProp = view.Props.FirstOrDefault(x => x.Id == cascadeTargetProp.Id);
                if (viewCascadeTargetProp == null)
                {
                    throw new MojenException($"Cascade target property '{cascadeTargetProp.Name}' is missing in the editor view.");
                }

                // If any of the cascade-from props has changed...
                Oo($"if (");

                int i = 0;
                foreach (var cascadeFromProp in cascadeTargetProp.CascadeFromProps)
                {
                    var viewCascadeFromProp = view.Props.FirstOrDefault(x => x.Id == cascadeTargetProp.Id);
                    if (viewCascadeFromProp == null)
                        throw new MojenException($"Cascade-from property '{cascadeTargetProp.Name}' is missing in the editor view.");

                    o($"ce.field === '{cascadeFromProp.Name}'");

                    if (++i < cascadeTargetProp.CascadeFromProps.Count)
                        o(" || ");
                }

                oO(") {");
                Push();

                // Set cascade target prop to NULL.
                O($"e.item.set('{cascadeTargetProp.Name}', null);");

                // If applicable then also set the foreign key to null.
                // Otherwise the referenced entity will not be fully cleared out.
                if (cascadeTargetProp.IsNavigation)
                {
                    O($"e.item.set('{cascadeTargetProp.Reference.ForeignKey.Name}', null);");
                }

                Pop();
                O("}");
            }
        }

        void GenOnEditing_OnPropChanged_TriggersCore(MiaTypeTriggerConfig[] triggers)
        {
            if (!triggers.Any())
                return;

            foreach (var trigger in triggers)
            {
                OB($"if (ce.field === '{trigger.ContextProp.FormedTargetPath}')");
                foreach (var map in trigger.Operations.Items.OfType<MiaPropSetterConfig>())
                {
                    O($"e.item.set('{map.Target.FormedTargetPath}', e.item.get('{map.Source.FormedTargetPath}') || null);");
                }
                End();
            }
        }

        void GenOnEditing_QueryReferencedObject(MojViewConfig view, bool canCreate)
        {
            if (view == null)
                return;

            var type = view.TypeConfig;

            var looseReferenceGroups =
                (from prop in view.Props
                 // Select all read-only (or lookup display) props with *loose* references.
                 where !prop.IsEditable || prop.Lookup.Is
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

            OB("e.item.bind('change', function (ce)");
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

                var onCreateTriggerPropSetters = GetQueryPropSettersOfOnCreateTriggers(view, canCreate, reference.ObjectType).ToArray();
                if (onCreateTriggerPropSetters.Any())
                {
                    // Merge
                    queryNodes = queryNodes.Merge(onCreateTriggerPropSetters.Select(x => x.Source).BuildDataGraph()).ToList();
                }

                var onPropChangedTriggerGraphNodes = GetQueryNodesOfPropChangedTriggers(view, reference.ObjectType).ToArray();
                if (onPropChangedTriggerGraphNodes.Any())
                {
                    // Merge
                    queryNodes = queryNodes.Merge(onPropChangedTriggerGraphNodes).ToList();
                }

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                // Build the OData $select and $expand expression.
                string expression = this.BuildODataSelectAndExpand(queryNodes);

                // Build OData query.
                var odataQuery = $"{App.Get<WebODataBuildConfig>().QueryPrefix}/{reference.ObjectPluralName}/{this.GetODataQueryFunc()}?{expression}";

                // On create triggers.
                var sourceAssignments = new List<string>();
                // Assign properties.
                foreach (var setter in onCreateTriggerPropSetters)
                    sourceAssignments.Add($"{{ t: '{setter.Target.FormedTargetPath}', s: '{setter.Source.FormedTargetPath}'}}");

                O($"if (ce.field === '{reference.ForeignKeyPath}') e.sender._setNestedItem(" +
                    $"'{reference.ObjectPath}', " +
                    $"'{reference.ForeignKeyPath}', " +
                    $"'{reference.ObjectType.Key.Name}', " +
                    $"'{odataQuery}', " +
                    $"[{sourceAssignments.Join(", ")}]);");

                // Example:
                // if (ce.field === 'ContractId') this._setNestedItem(
                //    'Contract', 'ContractId', 'Id',
                //    '/odata/Contracts/Ga.Query()?$select=Id,City,CountryStateId,CountryId,Street,ZipCode&$expand=CountryState($select=Id,DisplayName),Country($select=Id,DisplayName)',
                //    [{ t: 'Street', s: 'Street'}, { t: 'ZipCode', s: 'ZipCode'}]);
            }
            End(");"); // Change handler
        }

        void GenOnEditing_ExtendEditModel(MojViewConfig view)
        {
            var type = view.TypeConfig;

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

                OB($"e.item.set('is{vprop.Name}SelectorEnabled', function ()");

                var expression = sprop.DbAnno.Unique.GetParams()
                    .Select(per => $"this.get('{per.Prop.Name}') != null")
                    .Join(" && ");

                O($" return {expression};");

                End(");");
            }
        }

        IEnumerable<MiaPropSetterConfig> GetQueryPropSettersOfOnCreateTriggers(MojViewConfig view, bool canCreate, MojType queriedForeignType)
        {
            var type = view.TypeConfig.RequiredStore;
            queriedForeignType = queriedForeignType.RequiredStore;

            if (canCreate)
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

        IEnumerable<MojDataGraphNode> GetQueryNodesOfPropChangedTriggers(MojViewConfig view, MojType queriedForeignType)
        {
            var type = view.TypeConfig.RequiredStore;
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
                $"/{this.GetODataQueryFunc()}?$select={targetType.Key.Name},{targetProp.Name}";

            return url;
        }

    }
}
