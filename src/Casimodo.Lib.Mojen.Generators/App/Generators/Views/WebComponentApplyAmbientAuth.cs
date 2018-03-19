using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public sealed class WebComponentApplyAmbientAuth : AppPartGenerator
    {
        public WebComponentApplyAmbientAuth()
        {
            Stage = "Prepare";
        }

        protected override void GenerateCore()
        {
            var pages = App.GetItems<MojControllerViewConfig>()
                .Where(x => x.IsPage)
                .ToList();

            var views = App.GetItems<MojControllerViewConfig>()
                .Where(x => !x.IsPage && !x.IsInline)
                .ToList();

            foreach (var view in views)
            {
                var page = pages.FirstOrDefault(x => x.Controller == view.Controller && x.Group == view.Group);
                if (page == null)
                    // KABU TODO: IMPORTANT: Do we want to enforce all view groups to have a page view?
                    continue;

                if (!page.IsAuthAmbientForGroup)
                    continue;

                if (view.Permissions.Any())
                {
                    if (!view.IsAuthAmbientOverwritten)
                        throw new MojenException("This view has explicit auth assigned and ambient auth was configured to be overwritten.");

                    continue;
                }

                foreach (var perm in page.Permissions)
                    view.Permissions.Add(perm);
            }
        }
    }
}