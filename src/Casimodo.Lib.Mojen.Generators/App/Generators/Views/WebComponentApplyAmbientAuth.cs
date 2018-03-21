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

                ApplyAmbientAuth(page, view);
            }

            // Try to assign auth of details views without pages to their editor views.
            foreach (var detailsWithoutPage in App.GetItems<MojControllerViewConfig>()
                .Where(x =>
                    x.IsDetails &&
                    !x.IsAuthAmbientApplied))
            {
                var editor = views.SingleOrDefault(x =>
                    x.IsEditor &&
                    x.Controller == detailsWithoutPage.Controller &&
                    x.Group == detailsWithoutPage.Group);
                if (editor != null)
                    ApplyAmbientAuth(detailsWithoutPage, editor);
            }
        }

        void ApplyAmbientAuth(MojViewConfig source, MojViewConfig target)
        {
            if (!source.IsAuthAmbientForGroup)
                return;

            if (target.AuthPermissions.Any())
            {
                if (!target.IsAuthAmbientOverwritten)
                    throw new MojenException("This view has explicit auth assigned and ambient auth was configured to be overwritten.");

                return;
            }

            foreach (var perm in source.AuthPermissions)
                target.AuthPermissions.Add(perm);

            target.IsAuthAmbientApplied = true;
        }
    }
}