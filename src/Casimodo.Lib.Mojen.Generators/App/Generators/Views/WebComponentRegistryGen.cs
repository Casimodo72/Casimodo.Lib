using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public sealed class WebComponentRegistryGen : AppPartGenerator
    {
        protected override void GenerateCore()
        {
            var filePath = Path.Combine(
                App.Get<WebBuildConfig>().WebConfigurationDirPath,
                "WebComponentRegistry.generated.cs");

            PerformWrite(filePath, () =>
            {
                OUsing("System", "Casimodo.Lib.Auth", "Casimodo.Lib.Web.Auth");
                ONamespace("Ga.Web");
                O("public partial class {0}WebComponentRegistry : WebComponentRegistry",
                    App.Get<AppBuildConfig>().AppNamePrefix);
                Begin();

                O("public override void Configure()");
                Begin();

                Func<WebResultComponentInfo, int> roleToInt = (x) =>
                {
                    if (x.View.MainRoleName == "Page") return 1;
                    if (x.View.MainRoleName == "List") return 2;
                    if (x.View.MainRoleName == "Editor") return 3;
                    if (x.View.MainRoleName == "Details") return 4;
                    if (x.View.MainRoleName == "Lookup") return 5;
                    return 0;
                };

                var components = App.Get<WebResultBuildInfo>().Components
                    .Where(x => !x.View.IsInline)
                    .OrderBy(x => x.View.TypeConfig.Name)
                    .ThenBy(x => x.View.Group)
                    .ThenBy(x => x.View.MainRoleName)
                    .ToList();

                foreach (var item in components)
                {
                    Oo("Add(new UIComponentInfo {{ Part = {0}, " +
                        "Group = {1}, Role = {2}, Actions = {3}, " +
                        "Title = {4}, Url = {5}, " +
                        "Id = {6} }})",
                        MojenUtils.ToCsValue(item.View.GetPartName()),
                        MojenUtils.ToCsValue(item.View.Group),
                        MojenUtils.ToCsValue(item.View.MainRoleName),
                        MojenUtils.ToCsValue(BuildActions(item.View)),
                        MojenUtils.ToCsValue(item.View.GetDefaultTitle()),
                        MojenUtils.ToCsValue(BuildUrl(item.View.Url)),
                        MojenUtils.ToCsValue(item.View.Id));

                    if (item.View.AuthPermissions.Any())
                    {
                        Br();
                        Push();
                        foreach (var perm in item.View.AuthPermissions)
                        {
                            O(".SetRole({0}, {1}, {2})",
                                MojenUtils.ToCsValue(perm.Role),
                                MojenUtils.ToCsValue(perm.Permit),
                                MojenUtils.ToCsValue(perm.Deny));
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

                //O("/*");
                //foreach (var item in components.Where(x => x.View.IsPage))
                //{
                //    O("Set(CommonUserRole.Manager, item: {0}, viewRole: \"Page\", permit: \"View\");",
                //         MojenUtils.ToCsValue(item.View.TypeConfig.Name));
                //}
                //O("*/");
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
                url = url.Substring(0, idx);

            return url;
        }
    }
}