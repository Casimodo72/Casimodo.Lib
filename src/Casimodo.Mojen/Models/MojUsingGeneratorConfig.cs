using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojUsingGeneratorConfig
    {
        public Type Type { get; set; }

        [DataMember]
        string _typeQName;

        /// <summary>
        /// KABU TODO: IMPL serialization when needed.
        /// </summary>
        public List<object> Args { get; set; } = new List<object>();

        public void AddArgs(object args)
        {
            if (args == null)
                return;

            Args.Add(args);
        }

        public T GetArgs<T>()
            where T : class
        {
            if (Args == null)
                return null;

            return (T)Args.FirstOrDefault(x => x.GetType() == typeof(T));
        }

        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            _typeQName = Type?.AssemblyQualifiedName;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (_typeQName != null)
                Type = Type.GetType(_typeQName);
        }
    }
}