using System.IO;

namespace Casimodo.Lib.Mojen
{
    public class TypeKeysGen : DataLayerGenerator
    {
        public TypeKeysGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            if (DataConfig.IsOutputDisabled)
                return;

            PerformWrite(Path.Combine(DataConfig.DataPrimitiveDirPath, "TypeKeys.generated.cs"),
                () => GenerateTypeKeys());
        }

        public void GenerateTypeKeys()
        {
            OUsing("System");

            ONamespace(DataConfig.DataNamespace);

            O($"public static class {DataConfig.GetTypeKeysClassName()}");
            Begin();

            var ids = new List<Guid>();

            foreach (var type in App.GetTypes())
            {
                if (ids.Contains(type.Id.Value))
                    continue;

                ids.Add(type.Id.Value);

                O($"public static readonly Guid {type.Name} = new Guid(\"{type.Id}\");");
            }

            End();
            End();
        }
    }
}