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
                OUsing("System", "Casimodo.Lib.Web.Auth");
                ONamespace("Ga.Web");
                O("public partial class {0}WebComponentRegistry : WebComponentRegistry",
                    App.Get<AppBuildConfig>().AppNamePrefix);
                Begin();

                O("public override void Configure()");
                Begin();

                Func<WebResultComponentInfo, int> roleToInt = (x) =>
                {
                    if (x.Role == "Page") return 1;
                    if (x.Role == "List") return 2;
                    if (x.Role == "Editor") return 3;
                    if (x.Role == "Details") return 4;
                    if (x.Role == "Lookup") return 5;
                    return 0;
                };

                var components = App.Get<WebResultBuildInfo>().Components
                    .OrderBy(x => x.Item)
                    .ThenBy(x => x.Group)
                    .ThenBy(x => x.Role)
                    .ToList();

                foreach (var item in components)
                {
                    Oo("Add(new WebComponentRegItem {{ ItemName = {0}, " +
                        "ViewGroup = {1}, ViewRole = {2}, ViewActions = {3}, " +
                        "Title = {4}, HasComponent = {5}, Url = {6}, " +
                        "ViewControllerName = {7}, ViewControllerActionName = {8}, " +
                        "ViewId = {9} }})",
                        MojenUtils.ToCsValue(item.View.TypeConfig.Name),
                        MojenUtils.ToCsValue(item.Group),
                        MojenUtils.ToCsValue(item.Role),
                        MojenUtils.ToCsValue(BuildActions(item.View)),
                        MojenUtils.ToCsValue(item.View.Kind.Roles.HasFlag(MojViewRole.List) ? item.View.TypeConfig.DisplayPluralName : item.View.TypeConfig.DisplayName),
                        MojenUtils.ToCsValue(item.Name != null),
                        MojenUtils.ToCsValue(BuildUrl(item.Url)),
                        MojenUtils.ToCsValue(item.View.TypeConfig.PluralName),
                        MojenUtils.ToCsValue(item.View.ControllerActionName),
                        MojenUtils.ToCsValue(item.Id));

                    if (item.View.Permissions.Any())
                    {
                        Br();
                        Push();
                        foreach (var perm in item.View.Permissions)
                        {
                            O(".SetRoles({0}, {1}, {2})",
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

                O("/*");
                foreach (var item in components.Where(x => x.Role == "Page"))
                {
                    O("Set(CommonUserRole.Manager, item: {0}, viewRole: \"Page\", permit: \"View\");",
                         MojenUtils.ToCsValue(item.View.TypeConfig.Name));
                }
                O("*/");
            });
        }

        string BuildActions(MojViewConfig view)
        {
            var actions = "";
            if (view.Kind.Roles.HasFlag(MojViewRole.Editor))
            {
                if (view.CanCreate)
                    actions += ",Create";
                if (view.CanCreate)
                    actions += ",Modify";
                if (view.CanDelete)
                    actions += ",Delete";
            }
            else
                actions = "View";

            if (actions.StartsWith(","))
                actions = actions.Substring(1);

            return actions;
        }

        string BuildUrl(string url)
        {
            if (url == null)
                return null;

            if (url.StartsWith("/"))
                url = url.Substring(1);

            var idx = url.LastIndexOf("/Index");
            if (idx != -1)
                url = url.Substring(0, idx);

            return url;
        }
    }
}