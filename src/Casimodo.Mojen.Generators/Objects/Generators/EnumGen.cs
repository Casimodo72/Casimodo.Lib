using System.Collections.ObjectModel;
using System.IO;

namespace Casimodo.Lib.Mojen
{
    public class EnumGen : DataLayerGenerator
    {
        static readonly ReadOnlyCollection<string> _ValidEnumAttrs = new(new string[] {
            "DataContract", "EnumMember", "Display", "Description"
        });

        public EnumGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            foreach (var enu in App.GetTopTypes(MojTypeKind.Enum).Where(x => !x.WasGenerated))
                PerformWrite(Path.Combine(DataConfig.DataPrimitiveDirPath, enu.ClassName + ".generated.cs"),
                    () => Generate(enu));
        }

        public void Generate(MojType enu)
        {
            OUsing(App.Get<DataLayerConfig>().DataNamespaces);

            ONamespace(enu.Namespace);

            O($"[TypeIdentity(\"{enu.Id}\")]");
            O("[DataContract]");
            O("public enum {0}", enu.ClassName);
            Begin();
            MojProp member;
            var members = enu.GetLocalProps().ToList();
            for (int i = 0; i < members.Count; i++)
            {
                if (i > 0)
                    O();

                member = members[i];

                O("[EnumMember]");

                // Attributes
                foreach (var attr in member.Attrs.Where(x => _ValidEnumAttrs.Contains(x.Name)).OrderBy(x => x.Position).ThenBy(x => x.Name))
                {
                    O(BuildAttr(attr));
                }

                // Enum member
                O("{0} = {1},", member.Name, member.EnumValue.Value);
            }
            End();
            End();
        }
    }
}