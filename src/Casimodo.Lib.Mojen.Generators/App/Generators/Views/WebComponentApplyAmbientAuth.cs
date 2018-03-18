using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public sealed class WebComponentApplyAmbientAuth : AppPartGenerator
    {
        protected override void GenerateCore()
        {
            var pages = App.GetItems<MojControllerViewConfig>()
                .Where(x => x.Kind.Roles.HasFlag(MojViewRole.Page))
                .ToList();

            var components = App.GetItems<MojControllerViewConfig>()
                .Where(x => !x.Kind.Roles.HasFlag(MojViewRole.Page) && !x.IsInline)
                .ToList();

            foreach (var item in components)
            {
                var page = pages.FirstOrDefault(x => x.Controller == item.Controller && x.Group == item.Group);
                if (page == null)
                    continue;

                if (!page.IsAuthAmbientForGroup)
                    continue;

                if (item.Permissions.Any())
                {
                    if (!item.IsAuthAmbientOverwritten)
                        throw new MojenException("This view has explicit auth assigned and ambient auth was configured to be overwritten.");

                    continue;
                }

                foreach (var perm in page.Permissions)
                    item.Permissions.Add(perm);
            }
        }
    }
}