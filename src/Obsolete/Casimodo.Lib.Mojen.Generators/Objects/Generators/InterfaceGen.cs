﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // TODO: Rename to EntityInterfaceGen because it operates on entities only.
    // TODO: Add ModelInterfaceGen which operates on models only.
    public class InterfaceGen : ClassGen
    {
        public InterfaceGen()
        {
            Scope = "Context";
        }

        // REMOVE:public DataLayerConfig DataConfig { get; set; }

        protected override void GenerateCore()
        {
            DataConfig = App.Get<DataLayerConfig>();

            if (DataConfig == null) return;
            if (string.IsNullOrEmpty(DataConfig.InterfaceDirPath)) return;

            foreach (var iface in App.GetTypes(MojTypeKind.Interface))
            {
                PerformWrite(Path.Combine(App.Get<DataLayerConfig>().InterfaceDirPath, iface.ClassName + ".generated.cs"),
                    () => GenerateInterface(iface));
            }
        }

        public void GenerateInterface(MojType type)
        {
            // Operate on the entitiy.
            type = type.StoreOrSelf;

            OUsing(App.Get<DataLayerConfig>().DataNamespaces);

            ONamespace(type.Namespace);

            // Interface declaration

            O("public partial interface {0}{1}",
                type.ClassName,
                (type.EffectiveBaseClassName != null ? " : " + type.EffectiveBaseClassName : ""));

            // Interfaces
            if (type.Interfaces.Any())
            {
                if (type.HasBaseClass)
                    Oo("    , ");
                else
                    Oo("    : ");

                o(type.Interfaces.Select(x => x.Name).Join(", "));
                Br();
            }

            Begin();

            // Properties
            O();
            int i = -1;
            foreach (var prop in type.GetLocalProps(custom: false))
            {
                i++;

                if (i > 0)
                    O();

                OSummary(prop.Summary);

                O($"{GetPropertyType(type, prop)} {prop.Name} {{ get; set; }}");
            }

            End();
            End();
        }

        string GetPropertyType(MojType type, MojProp prop)
        {
            // Ensure that we reference an entity and not a view model here.
            if (prop.Type.IsMojType && prop.Type.TypeConfig.IsEntityOrModel())
                return prop.Type.TypeConfig.RequiredStore.Name;

            return prop.Type.Name;
        }
    }
}