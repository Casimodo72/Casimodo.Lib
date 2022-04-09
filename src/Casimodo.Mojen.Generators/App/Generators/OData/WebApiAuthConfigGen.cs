using System.IO;

namespace Casimodo.Mojen
{
    public class WebApiAuthConfigGen : WebPartGenerator
    {
        public WebApiAuthConfigGen()
        {
            Scope = "App";
        }

        IEnumerable<MojControllerConfig> GetControllers()
        {
            return App.GetItems<MojControllerConfig>();
        }

        protected override void GenerateCore()
        {
            // KABU TODO: IMPORTANT: Rename from WebApiAuthConfig to WebODataControllerAuthConfig
            //   in order to reflect that those auth rules are coming from the
            //   *Controller* configuration and that this is meant for *OData*.

            var filePath = Path.Combine(
                App.Get<WebAppBuildConfig>().WebAuthConfigurationDirPath,
                    "WebApiAuthConfig.generated.cs");

            var controllers = GetControllers().ToArray();

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

                    Oo($"builder.GetOrAddPart(\"{controller.TypeConfig.Name}\")");
                    foreach (var perm in controller.AuthPermissions)
                    {
                        o($".AddApiAction(\"{perm.Permit}\")");
                        o($".AuthRole(\"{perm.Role}\", \"{perm.Permit}\")");
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
