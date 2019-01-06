using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class DataViewModelLayerConfig : MojBase
    {
        [DataMember]
        public DataLayerConfig DataConfig { get; set; }

        [DataMember]
        public string DataViewModelNamespace { get; set; }

        [DataMember]
        public string DataViewModelDirPath { get; set; }

        [DataMember]
        public string DataViewModelAutoMapperDirPath { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class LegacyDataMappingConfig : MojBase
    {
        [DataMember]
        public string SourceTypesDescriptorFilePath { get; set; }

        [DataMember]
        public string[] IgnoredSourceTypes { get; set; } = new string[0];

        [DataMember]
        public string SourceDbContextName { get; set; }

        [DataMember]
        public string TargetDbContextName { get; set; }

        [DataMember]
        public string OutputDirPath { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class WebDataLayerConfig : MojBase
    {
        [DataMember]
        public string TypeScriptDataDirPath { get; set; }

        [DataMember]
        public string JavaScriptDataDirPath { get; set; }

        [DataMember]
        public string ODataNamespace { get; set; }

        // KABU TODO: REMOVE: Not used anymore
        //[DataMember]
        //public string AngularDataStorageDirPath { get; set; }

        // KABU TODO: REMOVE: Not used anymore
        //[DataMember]
        //public string AngularModuleName { get; set; }

        [DataMember]
        public string ScriptNamespace { get; set; }
    }

    // KABU TODO: Split into specialized configs.
    [DataContract(Namespace = MojContract.Ns)]
    public class DataLayerConfig : MojBase
    {
        [DataMember]
        public bool IsMetadataEnabled { get; set; } = true;

        [DataMember]
        public string IODataDynamicPropertiesAccessor { get; set; } = "IODataDynamicPropertiesAccessor";

        [DataMember]
        public string EntityDirPath { get; set; }

        [DataMember]
        public string ComplexTypeDirPath { get; set; }

        [DataMember]
        public string DataPrimitiveDirPath { get; set; }

        [DataMember]
        public string InterfaceDirPath { get; set; }

        [DataMember]
        public string DbDirPath { get; set; }

        [DataMember]
        public string DbContextDirPath { get; set; }

        [DataMember]
        public string DbInitializerDirPath { get; set; }

        [DataMember]
        public string DbRepositoryDirPath { get; set; }

        [DataMember]
        public string DbMigrationDirPath { get; set; }

        [DataMember]
        public string DbSeedRegistryDirPath { get; set; }

        [DataMember]
        public string DbSeedDirPath { get; set; }

        /// <summary>
        /// The dir path where binary DB content is put and read by the seeding machinery.
        /// </summary>
        [DataMember]
        public string DbSeedBinariesDirPath { get; set; }

        [DataMember]
        public bool NoConstructor { get; set; }

        [DataMember]
        public bool ExistsAlready { get; set; }

        [DataMember]
        public string DataNamespace { get; set; }

        [DataMember]
        public List<string> DataNamespaces { get; set; } = new List<string>();

        [DataMember]
        public string ModelNamespace { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string TypePrefix { get; set; }

        [DataMember]
        public string DbContextName { get; set; }

        [DataMember]
        public string MetaName { get; set; }

        [DataMember]
        public bool IsDbContextModelEnabled { get; set; }

        [DataMember]
        public bool DbContextUseMapping { get; set; }

        [DataMember]
        public string DbInitializerName { get; set; }

        [DataMember]
        [Obsolete("Use the DB migration seeder instead.")]
        public string DbSeederName { get; set; }

        [DataMember]
        public string DbRepositoryCoreName { get; set; }

        [DataMember]
        public string DbRepositoryName { get; set; }

        [DataMember]
        public string DbRepoOperationContextName { get; set; }

        [DataMember]
        public string DbRepoContainerName { get; set; }

        [DataMember]
        public string DbModelRepositoryCoreName { get; set; }

        public string DbContextConnectionStringName
        {
            get { return DataNamespace + "." + DbContextName; }
        }

        [DataMember]
        public MojType Tenant { get; set; }
    }
}