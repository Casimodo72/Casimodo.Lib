using System;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public static class CrossNetExtensions
    {
        public static MojModelPropBuilder UnidirManyToManyCollectionOf(this MojModelPropBuilder pbuilder,
          MojType itemType, string linkTypeGuid)
        {
            var app = pbuilder.App;

            if (app.IsDotNetCore())
            {
                var prop = pbuilder.PropConfig;

                var type = prop.DeclaringType.PluralName + "2" + itemType.PluralName;

                var atype = prop.DeclaringType; // e.g. Project
                var aprop = prop.DeclaringType.Name; // e.g. Project
                var aid = aprop + "Id"; // e.g. ProjectId

                var btype = itemType; // e.g. MoTag
                var bprop = prop.Name; // e.g. Tag
                var btypePlural = itemType.PluralName; // e.g. MoTags
                var bid = bprop + "Id"; // e.g. TagId

                // Add many-to-many link type.
                var m = app.CurrentBuildContext.AddModel(type)
                    .Id(linkTypeGuid);

                app.GetDotNetCoreOptions().ConfigureManyToManyLinkType?.Invoke(m);

                m.Store();

                m.Key();
                m.Prop(aprop).Type(atype, required: true);
                m.Prop(bprop).Type(btype, required: true);
                m.PropIndex().Store();
                m.Store(eb =>
                {
                    eb.Index(true, aprop, bprop);
                });

                var linkType = m.Build();

                prop.Name = "To" + prop.Name;

                pbuilder.ChildCollectionOf(linkType, backrefNew: false);
            }
            else
            {
                pbuilder.IndependenCollectionOf(itemType);
            }

            return pbuilder;
        }     
    }
}