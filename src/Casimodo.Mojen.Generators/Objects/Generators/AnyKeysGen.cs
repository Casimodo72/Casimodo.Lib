﻿using System.IO;

namespace Casimodo.Mojen
{
    public class AnyKeysGen : DataLayerGenerator
    {
        public AnyKeysGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
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