using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class JsAnyKeysGen : DataLayerGenerator
    {
        public JsAnyKeysGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();

            var configs = App.GetItems<MojAnyKeysConfig>().ToArray();
            if (!configs.Any())
                return;

            PerformWrite(Path.Combine(webConfig.JavaScriptDataDirPath, $"primitives.AnyKeys.generated.js"), () =>
            {
                OJsNamespace(webConfig.ScriptNamespace, () =>
                {
                    foreach (var config in configs)
                        GenerateAnyKeys(config);
                });

            });
        }

        public void GenerateAnyKeys(MojAnyKeysConfig config)
        {
            OJsClass(config.ClassName, true, () =>
            {
                foreach (var item in config.Items)
                    O($"this.{item.Key} = {MojenUtils.ToJsValue(item.Value)};");
            });
            O();
        }
    }
}