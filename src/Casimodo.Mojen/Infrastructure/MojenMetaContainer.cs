using Casimodo.Lib.Data;
using System.Runtime.Serialization;

namespace Casimodo.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojenMetaContainer
    {
        public MojenMetaContainer()
        {
            Items = [];
        }

        [DataMember]
        Dictionary<string, object> Items { get; set; }

        public void Add(MojBase item)
        {
            if (string.IsNullOrEmpty(item.MetadataId))
            {
                if (item is MojType type)
                    item.MetadataId = type.QualifiedClassName;

                if (item is MojValueSetContainer values)
                    item.MetadataId = "ValuesOf:" + values.TypeConfig.QualifiedClassName;
            }

            if (string.IsNullOrWhiteSpace(item.MetadataId)) throw new InvalidOperationException("No item metadata key.");

            Items.Add(item.MetadataId, item);
        }

        public IEnumerable<T> GetItems<T>()
        {
            return Items.Values.OfType<T>();
        }

        public T Get<T>(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            return (T)Items[id];
        }
    }
}
