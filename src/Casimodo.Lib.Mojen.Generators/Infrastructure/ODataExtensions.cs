﻿using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public static class MojenAppODataExtensions
    {
        public static string GetODataControllerName(this AppPartGenerator gen, MojType type)
        {
            return type.PluralName + "Controller";
        }

        public static string GetODataPath(this AppPartGenerator gen, MojType type)
        {
            return gen.App.Get<WebODataBuildConfig>().Path + "/" + type.PluralName;
        }

        public static string GetODataFunc(this AppPartGenerator gen, string function)
        {
            return gen.App.Get<WebODataBuildConfig>().Ns + "." + function;
        }

        public static string GetODataQueryFunc(this AppPartGenerator gen, MojType type)
        {
            return gen.GetODataPath(type) + "/" + gen.GetODataQueryFunc();
        }

        public static string GetODataOrderBy(this MojType type)
        {
            return type.GetProps()
                .Where(x => x.InitialSort.Is)
                .OrderBy(x => x.InitialSort.Index)
                .Select(x => x.InitialSort.Direction == MojOrderDirection.Ascending ? x.Name : x.Name + " desc")
                .Join(",");
        }

        public static string GetODataQueryFunc(this AppPartGenerator gen, bool distinct = false)
        {
            var config = gen.App.Get<WebODataBuildConfig>();
            var func = distinct ? config.QueryDistinct : config.Query;
            return gen.GetODataFunc(func);
        }

        public static string GetODataCreateUrl(this MojViewConfig view, string baseUrl)
        {
            if (!view.IsEditor)
                throw new MojenException("An editor view was expected.");

            // NOTE: For now all view groups can share the default OData create action.
            return baseUrl;
        }

        public static string GetODataUpdateUrlTemplate(this MojViewConfig view, string baseUrl)
        {
            if (!view.IsEditor)
                throw new MojenException("An editor view was expected.");

            var url = baseUrl + "({0})";

            if (view.Group != null)
                url += $"/{view.GetODataUpdateActionName()}";

            return url;
        }

        public static string GetODataCreateActionName(this MojViewConfig view)
        {
            if (!view.IsEditor)
                throw new MojenException("An editor view was expected.");

            // NOTE: For now all view groups can share the default OData create action.
            return "Post";

            //if (view.Group != null)
            //    return $"{view.Group}Create";
            //else
            //    return "Post";
        }

        public static string GetODataUpdateActionName(this MojViewConfig view)
        {
            if (!view.IsEditor)
                throw new MojenException("An editor view was expected.");

            if (view.Group != null)
                return $"{view.Group}Update";
            else
                return "Put";
        }

        // KABU TODO: Not used anywhere.
        public static string GetODataRouteForNextSequenceValue(this MojProp prop)
        {
            if (prop.IsModel()) throw new MojenException("The given property must not be a model.");

            var s = new StringBuilder();
            s.o(prop.GetNextSequenceValueMethodName());
            s.o("(");
            int i = 0;
            foreach (var per in prop.DbAnno.Unique.GetParams())
            {
                if (i++ > 0) s.o(",");
                s.o($"{per.Prop.Name}={{{per.Prop.Name}}}");
            }
            s.o(")");

            return s.ToString();
        }

        public static MojHttpRequestConfig CreateODataTransport(this AppPartGenerator gen, MojViewConfig view, MojViewConfig editorView)
        {
            var c = new MojHttpRequestConfig();

            c.ReadGraph = view.BuildDataGraphForRead();
            c.ModelProps = c.ReadGraph
                .OfType<MojPropDataGraphNode>().Select(x => x.Prop)
                // Add navigation props to collections.
                .Concat(c.ReadGraph
                    .OfType<MojReferenceDataGraphNode>()
                    .Where(x => x.SourceProp.Reference.IsToMany)
                    .Select(x => x.SourceProp))
                .ToArray();

            // OData URLs
            c.ODataBaseUrl = gen.GetODataPath(view.TypeConfig);
            c.ODataReadBaseUrl = c.ODataBaseUrl + $"/{gen.GetODataQueryFunc()}()?";
            // Build OData $expand expressions based on the used foreign properties in the view.
            c.ODataSelectUrl = c.ODataReadBaseUrl + gen.BuildODataSelectAndExpand(c.ReadGraph);
            if (!string.IsNullOrEmpty(view.CustomSelectFilter))
            {
                c.ODataSelectUrl += "&" + view.CustomSelectFilter;
            }
            c.ODataFilterUrl = c.ODataReadBaseUrl + "$select=";
            c.ODataCrudUrl = c.ODataBaseUrl;
            c.ODataCreateUrl = c.ODataCrudUrl;
            c.ODataUpdateUrlTemplate = c.ODataCrudUrl;
            c.ODataDeleteUrl = c.ODataCrudUrl;

            if (editorView != null)
            {
                c.ODataCreateUrl = editorView.GetODataCreateUrl(c.ODataBaseUrl);
                c.ODataUpdateUrlTemplate = editorView.GetODataUpdateUrlTemplate(c.ODataBaseUrl); ;
            }

            // Ajax URL
            // NOTE: Currently we don't use Ajax directly anywhere.
            c.AjaxUrl = null;

            return c;
        }

        public static string BuildODataSelectAndExpand(this AppPartGenerator gen, IEnumerable<MojDataGraphNode> nodes)
        {
            var s = new StringBuilder();

            // Build top-level OData $select query option.
            s.Append("$select=");
            var propNodes = nodes.OfType<MojPropDataGraphNode>().ToArray();
            int i = -1;
            foreach (var node in propNodes)
            {
                i++;
                if (i > 0) s.o(",");
                s.o(node.Prop.Name);
            }

            // Build OData $expand query options.
            var referenceNodes = nodes.OfType<MojReferenceDataGraphNode>().ToArray();
            BuildODataExpandCore(referenceNodes, 0, true, s);

            return s.ToString();
        }

        /// <summary>
        /// Generates an OData $expand expression based on the used properties in the views.
        /// I.e. only those properties will be requested which are actually needed for the views.
        /// </summary>
        /// <remarks>
        /// Example:
        /// $select=*
        /// 
        /// &$expand=
        ///     Customer($select=Id,Number,Name),
        ///     Company($select=Id,NameShort,Name),
        ///     ResponsiblePerson($select=Id,FullName),
        ///     BusinessContact($select=Id,FirstName,LastName,Mobile,EmailWork,FaxWork;
        ///         $expand=Salutation($select=Id,DisplayName)),
        /// DifferentInvoiceRecipient($select=Id,ZipCode,City,Street,Mobile,PhoneWork,EmailWork,FaxWork)
        /// </remarks>
        static void BuildODataExpandCore(IEnumerable<MojReferenceDataGraphNode> references, int depth, bool tail, StringBuilder s)
        {
            if (!references.Any())
                return;

            if (tail)
            {
                if (depth == 0)
                    s.o("&");
                else
                    s.o(";");
            }

            s.o("$expand=");

            var pos = 0;
            foreach (var reference in references)
            {
                pos++;

                if (pos > 1)
                    s.o(",");

                s.o(reference.SourceProp.Name);

                if (reference.TargetItems.Any())
                {
                    s.o("(");

                    // If x-to-many: filter out IsDeleted items.
                    // KABU TODO: VERY IMPORTANT: Actually sometimes we need to include IsDeleted items.
                    //   We need a configuration for this on the reference-definition.
                    if (reference.SourceProp.Reference.IsToMany)
                    {
                        var isDeletedMarker = reference.TargetType.FindDeletedMarker(MojPropDeletedMarker.Effective);
                        if (isDeletedMarker != null)
                        {
                            s.o($"$filter={isDeletedMarker.Name} eq false;");
                        }
                    }

                    var propPos = 0;
                    var propNodes = reference.TargetItems.OfType<MojPropDataGraphNode>().ToArray();
                    if (propNodes.Any())
                    {
                        s.o("$select=");
                        foreach (var propNode in propNodes)
                        {
                            propPos++;
                            s.o(propNode.Prop.Name);
                            if (propPos < propNodes.Length) s.o(",");
                        }
                    }

                    var refs = reference.TargetItems.OfType<MojReferenceDataGraphNode>().ToArray();
                    BuildODataExpandCore(refs, depth + 1, propPos != 0, s);

                    s.o(")");
                }
            }
        }
    }
}