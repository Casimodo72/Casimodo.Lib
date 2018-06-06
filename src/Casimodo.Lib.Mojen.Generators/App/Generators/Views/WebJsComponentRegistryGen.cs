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
                var components = App.Get<WebResultBuildInfo>().Components
                        .OrderBy(x => x.View.TypeConfig.Name)
                        .ToArray();

                if (!components.Any())
                    return;

                OScriptUseStrict();

                OJsNamespace(WebConfig.ScriptUINamespace, (nscontext) =>
                {
                    OJsClass(nscontext.Current, "ComponentRegistry",
                        isPrivate: false, isStatic: true,
                        extends: "casimodo.ui.ComponentRegistry",
                        constructor: () =>
                        {
                            O("this.namespace = {0};", MojenUtils.ToJsValue(WebConfig.ScriptUINamespace));
                        },
                        content: () =>
                        {
                            foreach (var item in components.Where(x => x.View?.Id != null))
                            {
                                O("fn.get{0}{1}{2} = function (options) {{ return this.getById({3}, options); }};",
                                    item.View.GetPartName(),
                                    item.View.MainRoleName,
                                    (item.View.Group != null ? "_" + item.View.Group : ""),
                                    MojenUtils.ToJsValue(item.View.Id));
                            }
                        });

                    O();
                    O("var reg = casimodo.ui.componentRegistry = {0}.ComponentRegistry;", nscontext.Current);

                    O();
                    foreach (var item in components)
                    {
                        O("reg.add({{ part: {0}, group: {1}, role: {2}, url: {3}, id: {4}, maxWidth: {5}, maxHeight: {6}, editorId: {7} }});",
                            MojenUtils.ToJsValue(item.View.TypeConfig.Name),
                            MojenUtils.ToJsValue(item.View.Group),
                            MojenUtils.ToJsValue(item.View.MainRoleName),
                            MojenUtils.ToJsValue(item.View.Url),
                            MojenUtils.ToJsValue(item.View.Id),
                            MojenUtils.ToJsValue(item.View.MaxWidth),
                            MojenUtils.ToJsValue(item.View.MaxHeight),
                            MojenUtils.ToJsValue(item.View.EditorView?.Id));
                    }
                });
            });
        }
    }
}