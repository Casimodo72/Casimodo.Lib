using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    public class MojBaseData : Dictionary<string, string>
    {
        public string Get(string name)
        {
            if (TryGetValue(name, out string value))
                return value;

            return null;
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public abstract class MojBase
    {
        [DataMember]
        public string MetadataId { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public abstract class MojBuilderBase : MojBase
    {
        public List<MojUsingGeneratorConfig> UsingGenerators { get; private set; }

        public T GetGeneratorConfig<T>()
            where T : class
        {
            return UsingGenerators.SelectMany(x => x.Args).FirstOrDefault(x => x != null && x is T) as T;
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public abstract class MojPartBase : MojBase, IMojenGenerateable
    {
        [DataMember]
        public List<MojUsingGeneratorConfig> UsingGenerators { get; private set; } = new List<MojUsingGeneratorConfig>();

        public List<MojAuthPermission> AuthPermissions { get; set; } = new List<MojAuthPermission>();

        public bool Uses(MojenGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException("generator");
            return UsingGenerators.Any(x => x.Type == generator.GetType());
        }

        public bool Uses<TGen>()
            where TGen : MojenGenerator
        {
            return UsingGenerators.Any(x => x.Type == typeof(TGen));
        }

        public T GetGeneratorConfig<T>()
            where T : class
        {
            return UsingGenerators.SelectMany(x => x.Args).FirstOrDefault(x => x != null && x is T) as T;
        }

        public virtual void Prepare(MojenApp app)
        {
            // NOP
        }
    }
}