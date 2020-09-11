namespace Casimodo.Lib.Mojen
{
    public class WebODataBuildConfig : MojenBuildConfig
    {
        public string Path { get; set; }

        public string Namespace { get; set; }

        public bool IsMethodNamespaceQualified { get; set; }

        public string Query { get; set; } = "Query";
        public string QueryDistinct { get; set; } = "QueryDistinct";

        // Web OData controllers
        public string WebODataControllerDirPath { get; set; }

        public string WebODataControllerBaseClass { get; set; }

        public string WebODataControllerNamespace { get; set; }

        /// <summary>
        /// Will generate physical entity delete operations instead of soft deletes.
        /// Intended for development purposes only. Use with care.
        /// </summary>
        public bool IsPhysicalDeletionEnabled { get; set; }

        public string EnableQueryAttributeName { get; set; } = "EnableQuery";
    }
}