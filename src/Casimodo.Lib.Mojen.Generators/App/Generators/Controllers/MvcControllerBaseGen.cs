using System;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class MvcControllerBaseGen : WebPartGenerator
    {
        protected override void GenerateCore()
        {
            foreach (var controller in App.GetItems<MojControllerConfig>().Where(x => x.Uses(this)))
                WriteToMvc(controller, GenerateControllerCore);
        }

        void GenerateControllerCore(MojControllerConfig controller)
        {
            ONamespace(controller.Namespace);
            OUsing("System", "System.Collections.Generic", "System.Data", "System.Data.Entity",
                "System.Linq", "System.Web", "System.Web.Mvc",
                "Casimodo.Lib", "Casimodo.Lib.Web", "Casimodo.Lib.Web.Auth",
                "System.Web.UI", // For OutputCacheLocation
                controller.TypeConfig.Namespace
            );

            O("[Authorize]");

            foreach (var attr in controller.Attrs)
            {
                O(BuildAttr(attr));
            }

            O("[RoutePrefix(\"{0}\")]", controller.PluralName);
            O("[Route(\"{action}/{id}\")]");
            O("public partial class {0} : Casimodo.Lib.Web.ControllerBase", controller.ClassName);
            Begin();

            GenerateController(controller);

            End(); // Controller
            End(); // Namespace
        }

        public virtual void GenerateController(MojControllerConfig controller)
        {
            // Stub
        }

        public void WriteToMvc(MojControllerConfig controller, Action<MojControllerConfig> callback)
        {
            string outputFilePath =
                Path.Combine(
                    App.Get<WebAppBuildConfig>().WebMvcControllersOutputDirPath,
                    controller.ClassName + ".generated.cs");

            PerformWrite(outputFilePath, () => callback(controller));
        }
    }
}