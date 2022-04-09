using System.IO;

namespace Casimodo.Mojen
{
    public sealed class WebComponentAuthConfigGen : AppPartGenerator
    {
        protected override void GenerateCore()
        {
            var filePath = Path.Combine(
                App.Get<WebAppBuildConfig>().WebAuthConfigurationDirPath,
                "WebComponentAuthConfig.generated.cs");

            PerformWrite(filePath, () =>
            {
                OUsing("System", "Casimodo.Lib.Auth", "Casimodo.Lib.Web.Auth");
                ONamespace("Ga.Web");
                O($"public partial class {App.Get<AppBuildConfig>().AppNamePrefix}WebComponentAuthConfig : WebComponentRegistry");
                Begin();

                O("public override void Configure()");
                Begin();

                //Func<WebResultComponentInfo, int> roleToInt = x =>
                //{
                //    if (x.View.MainRoleName == "Page") return 1;
                //    if (x.View.MainRoleName == "List") return 2;
                //    if (x.View.MainRoleName == "Editor") return 3;
                //    if (x.View.MainRoleName == "Details") return 4;
                //    if (x.View.MainRoleName == "Lookup") return 5;
                //    return 0;
                //};

                var components = App.Get<WebResultBuildInfo>().Components
                    .Where(x => !x.View.IsInline)
                    .OrderBy(x => x.View.TypeConfig.Name)
                    .ThenBy(x => x.View.Group)
                    .ThenBy(x => x.View.MainRoleName)
                    .ToList();

                foreach (var item in components)
                {
                    Oo($"Add(new UIComponentInfo {{ Part = {Moj.CS(item.View.GetPartName())}, " +
                        $"Group = {Moj.CS(item.View.Group)}, Role = {Moj.CS(item.View.MainRoleName)}, Actions = {Moj.CS(BuildActions(item.View))}, " +
                        $"Title = {Moj.CS(item.View.GetDefaultTitle())}, Url = {Moj.CS(BuildUrl(item.View.Url))}, " +
                        $"Id = {Moj.CS(item.View.Id)} }})");

                    if (item.View.AuthPermissions.Any())
                    {
                        Br();
                        Push();
                        foreach (var perm in item.View.AuthPermissions)
                        {
                            O($".SetRole({Moj.CS(perm.Role)}, {Moj.CS(perm.Permit)}, {Moj.CS(perm.Deny)})");
                        }
                        O(";");
                        Pop();
                    }
                    else
                        oO(";");

                }

                End();
                End();
                End();
            });
        }

        string BuildActions(MojViewConfig view)
        {
            var actions = "View";
            if (view.Kind.Roles.HasFlag(MojViewRole.Editor))
            {
                if (view.CanCreate)
                    actions += ",Create";
                if (view.CanModify)
                    actions += ",Modify";
                if (view.CanDelete)
                    actions += ",Delete";
            }
            //else
            //    actions = "View";

            //if (actions.StartsWith(","))
            //    actions = actions.Substring(1);

            return actions;
        }

        string BuildUrl(string url)
        {
            if (url == null)
                return null;

            var idx = url.LastIndexOf("/Index");
            if (idx != -1)
                url = url[..idx];

            return url;
        }
    }
}