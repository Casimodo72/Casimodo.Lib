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
            var filePath = Path.Combine(WebConfig.WebRegistryTypeScriptDirPath, "ComponentRegistry.generated.ts");

            PerformWrite(filePath, () =>
            {
                var components = App.Get<WebResultBuildInfo>().Components
                        .OrderBy(x => x.View.TypeConfig.Name)
                        .ToArray();

                if (!components.Any())
                    return;

                //OScriptUseStrict();

                OTsNamespace(WebConfig.ScriptUINamespace, (nscontext) =>
                {
                    ClassGen.OTsClass(ns: nscontext.Current, name: "ComponentRegistry",
                        export: true,
                        extends: "cmodo.ComponentRegistry",
                        constructor: () =>
                        {
                            O("this.ns = {0};", Moj.JS(WebConfig.ScriptUINamespace));
                        },
                        content: () =>
                        {
                            foreach (var item in components.Where(x => x.View?.Id != null))
                            {
                                O("get{0}{1}{2}(options?: any) {{ return this.getById({3}, options); }}",
                                    item.View.GetPartName(),
                                    item.View.MainRoleName,
                                    (item.View.Group != null ? "_" + item.View.Group : ""),
                                    Moj.JS(item.View.Id));
                            }
                        });

                    O();
                    O("export let componentRegistry = cmodo.componentRegistry = new ComponentRegistry;");
                    O("let reg = componentRegistry;");

                    O();
                    foreach (var item in components)
                    {
                        O("reg.add({{ part: {0}, group: {1}, role: {2}, url: {3}, id: {4}, minWidth: {5}, maxWidth: {6}, minHeight: {7}, maxHeight: {8}, maximize: {9}, editorId: {10} }});",
                            Moj.JS(item.View.TypeConfig.Name),
                            Moj.JS(item.View.Group),
                            Moj.JS(item.View.MainRoleName),
                            Moj.JS(item.View.Url),
                            Moj.JS(item.View.Id),
                            Moj.JS(item.View.MinWidth),
                            Moj.JS(item.View.MaxWidth),
                            Moj.JS(item.View.MinHeight),
                            Moj.JS(item.View.MaxHeight),
                            Moj.JS(item.View.IsMaximized),
                            Moj.JS(item.View.EditorView?.Id));
                    }
                });
            });
        }
    }
}