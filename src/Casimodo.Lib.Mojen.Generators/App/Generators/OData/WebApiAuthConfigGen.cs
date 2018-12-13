using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class WebApiAuthConfigGen : WebPartGenerator
    {
        public WebApiAuthConfigGen()
        {
            Scope = "App";
        }

        protected override void GenerateCore()
        {
            var filePath = Path.Combine(
                App.Get<WebAppBuildConfig>().WebAuthConfigurationDirPath,
                "WebApiAuthConfig.generated.cs");

            var controllers = App.GetItems<MojControllerConfig>().Where(x => x.Uses<ODataControllerGen>()).ToArray();

            PerformWrite(filePath, () =>
            {
                OUsing("Casimodo.Lib.Auth");
                ONamespace(App.Get<WebAppBuildConfig>().WebAuthNamespace);
                O("public static class WebApiAuthConfig");
                Begin();
                OB("public static void Configure(ActionAuthBuilder builder)");

                foreach (var controller in controllers)
                {
                    if (controller.AuthPermissions.Count == 0)
                        continue;

                    Oo("builder.GetOrAddPart(\"{0}\")", controller.TypeConfig.Name);
                    foreach (var perm in controller.AuthPermissions)
                    {
                        o(".AddApiAction(\"{0}\")", perm.Permit);
                        o(".AuthRole(\"{0}\", \"{1}\")",
                            perm.Role,
                            perm.Permit);

                    }
                    oO(";");
                }

                End();
                End();
                End();
            });
        }
    }
}
