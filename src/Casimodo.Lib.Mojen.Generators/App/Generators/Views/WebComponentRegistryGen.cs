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

                O("public void Configure()");
                Begin();

                Func<WebResultComponentInfo, int> roleToInt = (x) =>
                {
                    if (x.Role == "Main") return 1;
                    if (x.Role == "List") return 2;
                    if (x.Role == "Editor") return 3;
                    if (x.Role == "Details") return 4;
                    if (x.Role == "Lookup") return 5;
                    return 0;
                };

                foreach (var item in App.Get<WebResultBuildInfo>().Components
                .OrderBy(x => x.Role)
                    .OrderBy(x => x.Item)
                    .ThenBy(x => x.Group)
                    .ThenBy(x => x.Role))
                {
                    O("Add(new WebComponentRegItem {{ ItemName = {0}, ViewRole = {1}, ViewGroup = {2}, HasComponent = {3}, Url = {4}, ViewId = {5} }});",
                        MojenUtils.ToCsValue(item.View.TypeConfig.Name),
                        MojenUtils.ToCsValue(item.Role),
                        MojenUtils.ToCsValue(item.Group),
                        MojenUtils.ToCsValue(item.Name != null),
                        MojenUtils.ToCsValue(BuildUrl(item.Url)),
                        MojenUtils.ToCsValue(item.Id));
                }

                End();
                End();
                End();
            });
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