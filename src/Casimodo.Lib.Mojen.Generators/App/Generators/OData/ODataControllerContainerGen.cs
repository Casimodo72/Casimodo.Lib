using Casimodo.Lib.Data;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Generate a container file for all OData controllers.
    /// Used for registration of OData controllers.
    /// </summary>
    public class ODataControllerContainerGen : WebPartGenerator
    {
        public ODataControllerContainerGen()
        {
            Scope = "App";
        }

        WebODataBuildConfig ODataConfig { get; set; }

        protected override void GenerateCore()
        {
            ODataConfig = App.Get<WebODataBuildConfig>();

            var controllers = App.GetItems<MojControllerConfig>().Where(x => x.Uses<ODataControllerGen>()).ToArray();
            string allControllersFilePath = Path.Combine(ODataConfig.WebODataControllersDirPath, "_ODataControllerTypes.generated.cs");
            PerformWrite(allControllersFilePath, () =>
            {
                OUsing("System");                
                ONamespace(ODataConfig.WebODataServicesNamespace);
                O("public static class ODataControllerTypes");
                Begin();
                Oo("public static readonly Type[] Items = new[] { ");
                foreach (var controller in controllers)
                {
                    o("typeof({0}), ", this.GetODataControllerName(controller.TypeConfig));
                }
                o("};" + Environment.NewLine);
                End();
                End();
            });
        }
    }
}