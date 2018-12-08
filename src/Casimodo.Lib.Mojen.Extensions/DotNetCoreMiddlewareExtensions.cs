using System;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DotNetCoreMiddlewareOptions
    {
        public Action<MojModelBuilder> ConfigureManyToManyLinkType { get; set; }
    }

    public static class DotNetCoreMiddlewareExtensions
    {
        const string MiddlewareName = "DotNetCoreMiddleware";

        public static void UseDotNetCore(this MojenApp app, DotNetCoreMiddlewareOptions options)
        {
            Guard.ArgNotNull(options, nameof(options));

            app.Middlewares.Add(new MojenAppMiddlewareItem
            {
                Name = MiddlewareName,
                Options = options
            });
        }

        static DotNetCoreMiddlewareOptions GetOptions(MojenApp app)
        {
            return (DotNetCoreMiddlewareOptions)GetMiddleware(app).Options;
        }

        static MojenAppMiddlewareItem GetMiddleware(MojenApp app)
        {
            return app.Middlewares.FirstOrDefault(x => x.Name == MiddlewareName);
        }

        public static bool IsDotnetCore(this MojenApp app)
        {
            return app.Middlewares.Any(x => x.Name == MiddlewareName);
        }

        public static MojModelPropBuilder UnidirManyToManyCollectionOf(this MojModelPropBuilder pbuilder,
            MojType itemType, string linkTypeGuid)
        {
            var app = pbuilder.App;

            if (IsDotnetCore(pbuilder.App))
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

                GetOptions(app).ConfigureManyToManyLinkType?.Invoke(m);
             
                m.Store();

                m.Key();
                m.Prop(aprop).Type(atype, required: true);
                m.Prop(bprop).Type(btype, required: true);
                m.PropIndex();
                m.Store(eb =>
                {
                    eb.Index(true, aprop, bprop);
                });

                var linkType = m.Build();

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