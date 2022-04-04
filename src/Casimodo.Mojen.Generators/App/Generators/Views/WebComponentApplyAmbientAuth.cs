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

            // Try to assign auth of list views without pages to their detail/editor views.
            foreach (var list in App.GetItems<MojControllerViewConfig>()
                .Where(x => x.IsList && !x.IsAuthAmbientApplied))
            {
                foreach (var view in views.Where(x => (x.IsDetails || x.IsEditor) && IsSameGroup(list, x)))
                    ApplyAmbientAuth(list, view);
            }

            // Try to assign auth of details views without pages to their editor views.
            foreach (var details in App.GetItems<MojControllerViewConfig>()
                .Where(x => x.IsDetails && !x.IsAuthAmbientApplied))
            {
                ApplyAmbientAuth(details, views.SingleOrDefault(x => x.IsEditor && IsSameGroup(details, x)));
            }
        }

        bool IsSameGroup(MojControllerViewConfig a, MojControllerViewConfig b)
        {
            return a.Controller == b.Controller && a.Group == b.Group;
        }

        void ApplyAmbientAuth(MojViewConfig source, MojViewConfig target)
        {
            if (target == null)
                return;

            if (!source.IsAuthAmbientForGroup)
                return;

            if (target.AuthPermissions.Any())
            {
                if (!target.IsAuthAmbientOverwritten)
                    throw new MojenException("This view has explicit auth assigned but ambient " +
                        "auth not was configured to be overwritten.");

                return;
            }

            foreach (var perm in source.AuthPermissions)
                target.AuthPermissions.Add(perm);

            target.IsAuthAmbientApplied = true;
        }
    }
}