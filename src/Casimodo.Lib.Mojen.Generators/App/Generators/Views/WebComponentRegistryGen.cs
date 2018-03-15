using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public sealed class WebMvcComponentRegistryGen : AppPartGenerator
    {
        protected override void GenerateCore()
        {
            var filePath = Path.Combine(App.Get<WebBuildConfig>().WebConfigurationDirPath, "WebComponentRegistry.generated.cs");

            PerformWrite(filePath, () =>
            {
                OUsing("System", "Casimodo.Lib.Web");
                ONamespace("Ga.Web");
                O("public partial class WebComponentRegistry : WebComponentRegistryBase");
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

                var comparer = Comparer<WebResultComponentInfo>.Create((a, b) =>
                {
                    return a.Role.CompareTo(b.Role);
                    //return roleToInt(a).CompareTo(roleToInt(b));
                });

                foreach (var item in App.Get<WebResultBuildInfo>().Components
                .OrderBy(x => x.Role)
                    //.OrderBy(x => x.Item)
                    //.OrderBy(x => comparer)
                    //.ThenBy(x => x.Group)
                    //.ThenBy(x => x.Role)
                    //.ThenBy(x => comparer)
                    )
                {
                    O("Add(new WebComponentRegItem {{ Name = {0}, Role = {1}, Group = {2}, Url = {3}, JsTypeName = {4}, ViewId = {5} }});",
                        MojenUtils.ToCsValue(item.Item),
                        MojenUtils.ToCsValue(item.Role),
                        MojenUtils.ToCsValue(item.Group),
                        MojenUtils.ToCsValue(item.Url),
                        MojenUtils.ToCsValue(item.Name != null ? item.Namespace + "." + item.Name : null),
                        MojenUtils.ToCsValue(item.Id));
                }

                End();
            });
        }
    }
}