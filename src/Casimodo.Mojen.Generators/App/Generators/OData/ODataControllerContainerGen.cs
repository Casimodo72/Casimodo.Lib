using System.IO;

namespace Casimodo.Mojen
{
    /// <summary>
    /// Generate a container file for all OData controllers.
    /// Used for registration of OData controllers.
    /// </summary>
    [Obsolete]
    public class ODataControllerContainerGen : WebPartGenerator
    {
        public ODataControllerContainerGen()
        {
            throw new Exception();
            Scope = "App";
        }

        WebODataBuildConfig ODataConfig { get; set; }

        protected override void GenerateCore()
        {
            ODataConfig = App.Get<WebODataBuildConfig>();

            var controllers = App.GetItems<MojControllerConfig>().Where(x => x.Uses<ODataControllerGen>()).ToArray();
            string allControllersFilePath = Path.Combine(ODataConfig.WebODataControllerDirPath, "_ODataControllerTypes.generated.cs");
            PerformWrite(allControllersFilePath, () =>
            {
                OUsing("System");
                ONamespace(ODataConfig.WebODataControllerNamespace);
                O("public static class ODataControllerTypes");
                Begin();
                Oo("public static readonly Type[] Items = new[] { ");
                foreach (var controller in controllers)
                {
                    o($"typeof({this.GetODataControllerName(controller.TypeConfig)}), ");
                }
                o("};" + Environment.NewLine);
                End();
                End();
            });
        }
    }
}