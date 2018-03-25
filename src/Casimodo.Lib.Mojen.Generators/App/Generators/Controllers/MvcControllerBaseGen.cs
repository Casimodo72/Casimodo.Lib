using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class MvcControllerBaseGen : WebPartGenerator
    {
        protected override void GenerateCore()
        {
            foreach (var controller in App.GetItems<MojControllerConfig>().Where(x => x.Uses(this)))
                PerformWrite(controller, GenerateControllerCore);
        }

        public void OMvcActionAuthAttribute(MojViewConfig view)
        {
            if (view.IsAuthEnabled)
            {
                O("[MvcActionAuth(Part = \"{0}\", Group = {1}, VRole = \"{2}\")]",
                    view.TypeConfig.Name,
                    MojenUtils.ToCsValue(view.Group),
                    view.MainRoleName);
            }
        }

        public void OOutputCacheAttribute()
        {
            if (WebConfig.OutputCache.IsEnabled)
            {
                O("[CustomOutputCache(CacheProfile = \"{0}\"{1})]",
                    WebConfig.OutputCache.CacheProfile,
                    WebConfig.OutputCache.Revalidate ? ", Revalidate = true" : "");
            }
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
    }
}