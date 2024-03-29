﻿using System.Xml.Linq;

namespace Casimodo.Mojen
{
    public static class MojenWebAppExtensions
    {
        public static MojControllerConfig GetControllerFor(this MojenApp app, MojType type)
        {
            var controller = app.GetItems<MojControllerConfig>().FirstOrDefault(x => x.TypeConfig == type);
            if (controller == null)
                throw new MojenException($"Controller not found for type '{type.ClassName}'.");

            return controller;
        }
    }

    public class MojControllerConfig : MojPartBase
    {
        public MojControllerConfig(string pluralName)
        {
            PluralName = pluralName;
            ClassName = pluralName + "Controller";
            CanDelete = true;
        }

        public MojType TypeConfig { get; set; }

        public IEnumerable<MojViewProp> AllProperties
        {
            get { return Views.SelectMany(x => x.Props); }
        }

        //public string Name { get; set; }
        public string PluralName { get; set; }

        public string ClassName { get; set; }

        public bool HasViewWithRole(MojViewRole role)
        {
            return Views.Any(x => x.Kind.Roles.HasFlag(role));
        }

        public bool CanDelete { get; set; }

        public List<MojViewConfig> Views { get; private set; } = [];

        public List<MojAttr> Attrs { get; private set; } = [];

        public IEnumerable<string> GetViewGroups()
        {
            return Views.GroupBy(x => x.Group).Select(x => x.Key);
        }

        public MojViewConfig GetAnyEditorView()
        {
            return Views.FirstOrDefault(x => x.Kind.Roles.HasFlag(MojViewRole.Editor));
        }

        public MojViewConfig GetEditorView(string group = null)
        {
            return Views.FirstOrDefault(x => x.Group == group && x.Kind.Roles.HasFlag(MojViewRole.Editor));
        }

        public MojViewConfig GetPageView(string group)
        {
            return Views.SingleOrDefault(x => x.Group == group && x.IsPage);
        }

        public IEnumerable<MojViewConfig> GetPageViews()
        {
            return Views.Where(x => x.IsPage);
        }

        public MojViewConfig GetDetailsView(string group = null)
        {
            var view = Views.FirstOrDefault(x => x.Group == group && x.Kind.Roles.HasFlag(MojViewRole.Details));
            if (view != null)
                return view;

            var index = GetPageView(group);
            if (index != null)
            {
                view = index.InlineDetailsView;
                if (view != null)
                    return view;
            }

            return view;
        }

        IEnumerable<MojViewProp> GetAllViewPropsDeep(IEnumerable<MojViewConfig> views)
        {
            foreach (var view in views)
            {
                foreach (var prop in view.Props)
                {
                    yield return prop;

                    // Yield properties of sub-views.
                    if (prop.ContentView != null)
                    {
                        foreach (var p in GetAllViewPropsDeep(Enumerable.Repeat(prop.ContentView, 1)))
                            yield return p;
                    }
                }
            }
        }

        public MojProp[] GetAllPropsDistinctForRead(MojViewConfig[] views)
        {
            // Get the view-properties actually used in the views.            
            var props = GetAllViewPropsDeep(views)
                // Don't read properties which are intended for input only.
                .Where(x => !x.IsInputOnly)
                .Cast<MojProp>().ToList();

            // Insert mandatory key property if missing.
            if (!props.Any(x => x.FormedTargetPath == TypeConfig.Key.Name))
                props.Insert(0, TypeConfig.Key);

            // Remove duplicates.
            // Prefer edit properties to read properties, because the edit-properties hold
            // information needed to generate the edit data-model elsewhere.
            // Also prefer editable properties over read-only properties.           
            MojProp duplicate;
            MojViewProp vprop, vduplicate;
            foreach (var prop in props.ToArray())
            {
                if (!props.Contains(prop))
                    // This one was already removed.
                    continue;

                duplicate = props.FirstOrDefault(x => x != prop && x.FormedTargetPath == prop.FormedTargetPath);
                if (duplicate == null)
                    continue;

                vprop = prop as MojViewProp;
                if (vprop != null)
                {
                    if (vprop.View.IsEditor)
                    {
                        vduplicate = (MojViewProp)duplicate;

                        // Check: only one edit view per view-group allowed.
                        if (vprop.View != vduplicate.View && vduplicate.View.IsEditor)
                        {
                            // KABU TODO: Move this constraint to controller/view validation layer.
                            throw new MojenException($"Multiple edit views in the same view-group are not allowed.");
                        }

                        // Prefer editable properties to read-only properties.
                        if (duplicate.IsEditable && !prop.IsEditable)
                            props.Remove(prop);
                        else
                            props.Remove(duplicate);
                    }
                    else
                        props.Remove(prop);
                }
            }

            // KABU TODO: IMPL? predicates at OData query level.
            //var predicates = viewProps.Where(x => x.Predicate != null).Select(x => x.Model).ToList();            

            // KABU TODO: IMPORTANT: This checks only the top-level properties and not the whole tree.
            // Filter out non-exposable properties.
            var exposableProps = TypeConfig.GetExposableSchemaProps().Select(x => x.Name).ToList();
            // KABU TODO: How to also ensure that only exposable *navigated-to* properties are used?
            foreach (var prop in props)
            {
                if (prop.DeclaringType.IsComplex())
                    // Pass through non-entity props.
                    continue;

                if (!exposableProps.Contains(prop.Name))
                    throw new MojenException($"The property '{prop.Name}' is not exposable, thus must no included in read operations.");
            }

            return props.ToArray();
        }

        public MojDataGraphNode[] BuildDataGraphForRead(MojViewConfig[] views)
        {
            return GetAllPropsDistinctForRead(views)
                .BuildDataGraph(includeKey: true, includeForeignKey: true)
                .ToArray();
        }

        public XElement BuildDataGraphMaskForUpdate(string viewGroup = null)
        {
            // KABU TODO: IMPORTANT: Exclude read-only properties like CreatedOn, ModifiedOn, etc.

            // Operate on the entity type.
            var type = TypeConfig.RequiredStore;

            var elem = new XElement("MojDataGraphMask",
                new XAttribute("Type", type.QualifiedClassName));
 
            var editorView = GetEditorView(viewGroup);
            if (editorView == null)
                return elem;
          
            // KABU TODO: IMPORTAN: Ignore read-only properties of nested objects.
            var properties = editorView.Props
                // Ignore read-only top-level properties.
                .Where(x => x.IsEditable)
                .ToList();

            if (!properties.Any())
                return elem;

            elem.Add(
                BuildDataMaskCore(
                    properties.BuildDataGraph()));

            return elem;
        }

        // KABU TODO: Move to data mask lib.
        IEnumerable<XElement> BuildDataMaskCore(IEnumerable<MojDataGraphNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is MojReferenceDataGraphNode reference)
                {
                    // Operate on entity types.
                    var targetType = reference.TargetType.RequiredStore;

                    if (reference.SourceProp.Reference.IsToMany)
                    {
                        // Collections
                        // KABU TODO: Currently we only support updates of independent collections.
                        if (reference.SourceProp.Reference.Independent)
                        {
                            yield return new XElement("Ref",
                                new XAttribute("Name", reference.SourceProp.Name),
                                new XAttribute("Binding", reference.SourceProp.Reference.Binding),
                                new XAttribute("Multiplicity", reference.SourceProp.Reference.Multiplicity),
                                //new XAttribute("ForeignKey", reference.SourceProp.Reference.ForeignKey.Name),
                                new XElement("To", new XAttribute("Type", targetType.QualifiedClassName)));
                        }
                    }
                    else
                    {
                        yield return new XElement("Ref",
                            new XAttribute("Name", reference.SourceProp.Name),
                            new XAttribute("Binding", reference.SourceProp.Reference.Binding),
                            new XAttribute("Multiplicity", reference.SourceProp.Reference.Multiplicity),
                            new XAttribute("ForeignKey", reference.SourceProp.Reference.ForeignKey.Name),
                            new XElement("To",
                                new XAttribute("Type", targetType.QualifiedClassName),
                                BuildDataMaskCore(reference.TargetItems)));
                    }
                }
                else
                {
                    yield return new XElement("Prop",
                        new XAttribute("Name", (node as MojPropDataGraphNode).Prop.Name));
                }
            }
        }

        public override string ToString()
        {
            return ClassName;
        }
    }
}