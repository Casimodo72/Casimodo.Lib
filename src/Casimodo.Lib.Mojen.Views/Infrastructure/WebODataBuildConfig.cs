namespace Casimodo.Lib.Mojen
{
    public class WebODataBuildConfig : MojenBuildConfig
    {
        public string Path { get; set; }

        public string Ns { get; set; }

        public string Query { get; set; } = "Query";
        public string QueryDistinct { get; set; } = "QueryDistinct";

        // Web OData controllers
        public string WebODataControllersDirPath { get; set; }

        public string WebODataControllerBaseClass { get; set; }

        public string WebODataServicesNamespace { get; set; }

        // Web OData lookup controllers
        public string WebODataLookupControllersDirPath { get; set; }

        public string WebODataLookupServicesNamespace { get; set; }

        /// <summary>
        /// Will generate physical entity delete operations instead of soft deletes.
        /// Intended for development purposes only. Use with care.
        /// </summary>
        public bool IsPhysicalDeletionEnabled { get; set; }
    }
}