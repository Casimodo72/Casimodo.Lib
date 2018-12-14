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

        protected virtual string[] GetNamespaces(MojControllerConfig controller)
        {
            return new string[]
            {
                "System", "System.Collections.Generic", 
                    "System.Data", "System.Data.Entity",
                    "System.Linq", "System.Web", "System.Web.Mvc",
                    "Casimodo.Lib", "Casimodo.Lib.Web", "Casimodo.Lib.Web.Auth",
                    "System.Web.UI", // For OutputCacheLocation
                    controller.TypeConfig.Namespace
            };
        }

        protected virtual void OControllerAttrs(MojControllerConfig controller)
        {
            O("[RoutePrefix(\"{0}\")]", controller.PluralName);
            O("[Route(\"{action}/{id}\")]");
        }

        void GenerateControllerCore(MojControllerConfig controller)
        {
            ONamespace(WebConfig.WebMvcControllerNamespace);
            OUsing(GetNamespaces(controller));

            O("[Authorize]");

            foreach (var attr in controller.Attrs)
            {
                O(BuildAttr(attr));
            }

            OControllerAttrs(controller);
            O("public partial class {0} : Casimodo.Lib.Web.MvcControllerBase", controller.ClassName);
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
                    App.Get<WebAppBuildConfig>().WebMvcControllerOutputDirPath,
                    controller.ClassName + ".generated.cs");

            PerformWrite(outputFilePath, () => callback(controller));
        }
    }
}