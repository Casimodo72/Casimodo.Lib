using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class AnyKeysGen : DataLayerGenerator
    {
        public AnyKeysGen()
        {
            Scope = "Context";
        }

        public DataLayerConfig DataConfig { get; set; }

        protected override void GenerateCore()
        {
            DataConfig = App.Get<DataLayerConfig>();

            foreach (var config in App.GetItems<MojAnyKeysConfig>())
            {
                PerformWrite(Path.Combine(DataConfig.DataPrimitiveDirPath, $"{config.ClassName}.generated.cs"),
                    () => GenerateAnyKeys(config));
            }
        }

        public void GenerateAnyKeys(MojAnyKeysConfig config)
        {
            OUsing("System");

            ONamespace(DataConfig.DataNamespace);

            O($"public static class {config.ClassName}");
            Begin();

            foreach (var item in config.Items)
            {
                O($"public static readonly {config.ValueType.Name} {item.Key} = {GetConstructor(config, item)};");
            }

            End();

            if (config.UseInstance)
            {
                O();
                O($"public class {config.ClassName}Instance");
                Begin();

                foreach (var item in config.Items)
                    O($"public {config.ValueType.Name} {item.Key} {{ get {{ return {config.ClassName}.{item.Key}; }} }}");

                End();
            }


            End();
        }

        string GetConstructor(MojAnyKeysConfig config, MojAnyKeyItemConfig item)
        {
            if (config.ValueType == typeof(Guid))
                return $"new Guid(\"{item.Value}\")";

            return item.Value.ToString();
        }
    }
}