using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojenMetaContainer
    {
        public MojenMetaContainer()
        {
            Items = new Dictionary<string, object>();
        }

        [DataMember]
        Dictionary<string, object> Items { get; set; }

        public void Add(MojBase item)
        {
            if (string.IsNullOrEmpty(item.MetadataId))
            {
                var type = item as MojType;
                if (type != null)
                    item.MetadataId = type.QualifiedClassName;

                var values = item as MojValueSetContainer;
                if (values != null)
                    item.MetadataId = "ValuesOf:" + values.TargetType.QualifiedClassName;
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
