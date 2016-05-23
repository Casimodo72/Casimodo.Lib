﻿using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public static class MojenWebAppExtensions
    {
        public static ControllerConfig GetControllerFor(this MojenApp app, MojType type)
        {
            var controller = app.GetItems<ControllerConfig>().FirstOrDefault(x => x.TypeConfig == type);
            if (controller == null)
                throw new MojenException($"Controller not found for type '{type.ClassName}'.");

            return controller;
        }
    }

    public class ControllerConfig : MojPartBase
    {
        public ControllerConfig(string pluralName)
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

        public string Namespace { get; set; }

        //public string Name { get; set; }
        public string PluralName { get; set; }

        public string ClassName { get; set; }

        public bool HasRole(MojViewRole role)
        {
            return Views.Any(x => x.Kind.Roles.HasFlag(role));
        }

        public bool CanDelete { get; set; }

        public List<MojViewConfig> Views { get; private set; } = new List<MojViewConfig>();

        public List<MojAttr> Attrs { get; private set; } = new List<MojAttr>();

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

        public MojViewConfig GetIndexView(string group = null)
        {
            return Views.SingleOrDefault(x => x.Group == group && x.Kind.Roles.HasFlag(MojViewRole.Index));
        }

        public MojViewConfig GetDetailsView(string group = null)
        {
            var view = Views.FirstOrDefault(x => x.Group == group && x.Kind.Roles.HasFlag(MojViewRole.Details));
            if (view != null)
                return view;

            var index = GetIndexView(group);
            if (index != null)
            {
                view = index.InlineDetailsView;
                if (view != null)
                    return view;
            }

            return view;
        }

        public MojDataGraphNode[] BuildDataGraphForRead(string viewGroup)
        {
            // Filter out non-exposable properties.
            var exposableProps = TypeConfig.GetExposableSchemaProps().Select(x => x.Name).ToList();
            // KABU TODO: How to also ensure that only exposable *navigated-to* properties are used?

            // Get the properties actually used in the views.            
            var viewProps = Views.Where(x => x.Group == viewGroup).SelectMany(x => x.Props).ToList();
            var props = viewProps.Select(x => x.Model).ToList();
            var predicates = viewProps.Where(x => x.Predicate != null).Select(x => x.Model).ToList();

            // Expand related file reference properties.
            props.AddRange(props.ToArray().Where(x => x.FileRef.Is).SelectMany(x => x.AutoRelatedProps));

            foreach (var prop in props)
            {
                if (!exposableProps.Contains(prop.Name))
                    throw new MojenException($"The property '{prop.Name}' is not exposable, thus must no included in read operations.");
            }

            // Insert mandatory key property.
            props.Insert(0, TypeConfig.Key);

            return props.BuildDataGraph(includeKey: true, includeForeignKey: true).ToArray();
        }

        public XElement BuildDataGraphMaskForUpdate(string viewGroup = null)
        {
            // Operate on the entity type.
            var type = TypeConfig.RequiredStore;

            var elem = new XElement("MojDataGraphMask",
                new XAttribute("Type", type.QualifiedClassName));

            var editorView = GetEditorView(viewGroup);
            if (editorView == null)
                return elem;

            // KABU TODO: In which case we don't want to add read-only or informational properties?
            //   I.e. sometimes properties are display in the editor view
            //   just for informational purposes and are not intended to be part of an update operation.
            var properties = editorView.Props;
            if (!properties.Any())
                return elem;

            elem.Add(
                BuildDataMaskCore(
                    properties.BuildDataGraph()));

            return elem;
        }

        IEnumerable<XElement> BuildDataMaskCore(IEnumerable<MojDataGraphNode> nodes)
        {
            foreach (var node in nodes)
            {
                var reference = node as MojReferenceDataGraphNode;

                if (reference != null)
                {
                    if (reference.SourceProp.Reference.IsToMany)
                    {
                        // KABU TODO: IMPORTANT: REVISIT: Currently we don't support updates of collection properties.
                        continue;
                    }

                    // Operate on entity types.
                    var targetType = reference.TargetType.RequiredStore;

                    yield return new XElement("Ref",
                        new XAttribute("Name", reference.SourceProp.Name),
                        new XAttribute("Binding", reference.SourceProp.Reference.Binding),
                        new XAttribute("Cardinality", reference.SourceProp.Reference.Cardinality),
                        new XAttribute("ForeignKey", reference.SourceProp.Reference.ForeignKey.Name),
                        new XElement("To",
                            new XAttribute("Type", targetType.QualifiedClassName),
                            BuildDataMaskCore(reference.TargetItems)));
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