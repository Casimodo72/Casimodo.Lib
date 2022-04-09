using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    public enum MiaTypeTriggerEventKind
    {
        None = 0,
        Create = 1,
        Update = 2,
        Delete = 3,
        PropChanged = 4
    }

    public enum MiaTypeOpKind
    {
        None = 0,
        Create = 1,
        Update = 2,
        Delete = 3,
    }

    public enum MiaTriggerScenario
    {
        None = 0,
        Repository = 1 << 0,
        ViewModel = 1 << 1,
        All = Repository | ViewModel
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MiaTypeTriggerConfig : MojBase
    {
        [DataMember]
        public MiaTriggerScenario ForScenario { get; set; } = MiaTriggerScenario.All;

        [DataMember]
        public MojType ContextType { get; set; }

        [DataMember]
        public MiaTypeTriggerEventKind Event { get; set; } = MiaTypeTriggerEventKind.None;

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public MiaTypeOperationsConfig Operations { get; set; } = new MiaTypeOperationsConfig();

        //

        [DataMember]
        public MojProp ContextProp { get; set; }


        [DataMember]
        public MojCrudOp CrudOp { get; set; } = MojCrudOp.None;

        [DataMember]
        public MojType TargetType { get; set; }

        [DataMember]
        public MojMultiplicity Multiplicity { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MiaTypeOperationConfig : MojBase
    { }


    [DataContract(Namespace = MojContract.Ns)]
    public class MiaPropSetterConfig : MiaTypeOperationConfig
    {
        [DataMember]
        public MojProp Target { get; set; }

        [DataMember]
        public bool IsNativeSource { get; set; }

        [DataMember]
        public MojProp Source { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MiaTypeOperationsConfig : MojBase
    {
        [DataMember]
        public List<MiaTypeOperationConfig> Items { get; private set; } = new List<MiaTypeOperationConfig>();

        [DataMember]
        public string FactoryFunctionCall { get; set; }

        [DataMember]
        public string MappingFunctionName { get; set; }
    }
}