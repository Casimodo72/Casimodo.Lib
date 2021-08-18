namespace Casimodo.Lib.Mojen
{
    public class WebODataBuildConfig : MojenBuildConfig
    {
        /// <summary>
        /// The prefix expression which is prepended in the controller's route
        /// (e.g. "\"odata/\"", or "SomeGlobalConfig.SomePrefix").
        /// Currently mandatory.
        /// </summary>
        public string ControllerRoutePrefixExpression { get; set; } = "\"odata/\"";

        /// <summary>
        /// The OData URL prefix which is prepended in the query.
        /// Currently mandatory.
        /// </summary>
        public string QueryPrefix { get; set; } = "/odata";

        /// <summary>
        /// The OData namespace used for OData actions and functions.
        /// Currently mandatory.
        /// </summary>
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