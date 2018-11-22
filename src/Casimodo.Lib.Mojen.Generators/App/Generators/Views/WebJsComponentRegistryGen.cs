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
                            O("this.ns = {0};", MojenUtils.ToJsValue(WebConfig.ScriptUINamespace));
                        },
                        content: () =>
                        {
                            foreach (var item in components.Where(x => x.View?.Id != null))
                            {
                                O("get{0}{1}{2}(options?: any) {{ return this.getById({3}, options); }}",
                                    item.View.GetPartName(),
                                    item.View.MainRoleName,
                                    (item.View.Group != null ? "_" + item.View.Group : ""),
                                    MojenUtils.ToJsValue(item.View.Id));
                            }
                        });

                    O();
                    O("var reg = cmodo.componentRegistry = new ComponentRegistry;");

                    O();
                    foreach (var item in components)
                    {
                        O("reg.add({{ part: {0}, group: {1}, role: {2}, url: {3}, id: {4}, minWidth: {5}, maxWidth: {6}, minHeight: {7}, maxHeight: {8}, maximize: {9}, editorId: {10} }});",
                            MojenUtils.ToJsValue(item.View.TypeConfig.Name),
                            MojenUtils.ToJsValue(item.View.Group),
                            MojenUtils.ToJsValue(item.View.MainRoleName),
                            MojenUtils.ToJsValue(item.View.Url),
                            MojenUtils.ToJsValue(item.View.Id),
                            MojenUtils.ToJsValue(item.View.MinWidth),
                            MojenUtils.ToJsValue(item.View.MaxWidth),
                            MojenUtils.ToJsValue(item.View.MinHeight),
                            MojenUtils.ToJsValue(item.View.MaxHeight),
                            MojenUtils.ToJsValue(item.View.IsMaximized),
                            MojenUtils.ToJsValue(item.View.EditorView?.Id));
                    }
                });
            });
        }
    }
}