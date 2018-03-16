using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public sealed class WebJsComponentRegistryGen : WebPartGenerator
    {
        protected override void GenerateCore()
        {
            var filePath = Path.Combine(WebConfig.WebRegistryJavaScriptDirPath, "component.registry.generated.js");

            PerformWrite(filePath, () =>
            {
                OScriptUseStrict();

                OJsNamespace(WebConfig.ScriptUINamespace, (nscontext) =>
                {                   
                    O("var reg = casimodo.ui.componentRegistry;");
                    O();
                    foreach (var item in App.Get<WebResultBuildInfo>().Components
                        .OrderBy(x => x.Item))
                    {
                        O("reg.add({{ item: {0}, role: {1}, group: {2}, url: {3}, type: {4}, id: {5} }});",                          
                            MojenUtils.ToJsValue(item.Item),
                            MojenUtils.ToJsValue(item.Role),
                            MojenUtils.ToJsValue(item.Group),
                            MojenUtils.ToJsValue(item.Url),
                            MojenUtils.ToJsValue(item.Name != null ? item.Namespace + "." + item.Name : null),
                            MojenUtils.ToJsValue(item.Id));
                    }
                });
            });
        }
    }
}