namespace Casimodo.Lib.Mojen
{
    public abstract class EntityFromDbTransformationGenBase : DataLayerGenerator
    {
        public EntityFromDbTransformationGenBase()
        {
            Scope = "Context";
        }

        public MojGlobalDataSeedConfig MainSeedConfig { get; set; }
        public EntityFromDbTransformationOptions Options { get; set; }

        protected override void GenerateCore()
        {
            MainSeedConfig = App.Get<MojGlobalDataSeedConfig>(required: false);

            if (MainSeedConfig == null || !MainSeedConfig.IsDbImportEnabled ||
                string.IsNullOrEmpty(MainSeedConfig.DbImportOutputXmlDirPath) ||
                string.IsNullOrEmpty(MainSeedConfig.DbImportConnectionString))
                return;

            GenerateExport();
        }

        public abstract void GenerateExport();

        // Helpers

        public string AddOrderBy(string query)
        {
            var orderBy = !string.IsNullOrWhiteSpace(Options.OrderBy)
               ? Options.OrderBy.Split(",").Select(x => $"[{x.Trim()}]").Join(", ")
               : null;
            if (!string.IsNullOrWhiteSpace(orderBy))
                query += $" order by {orderBy}";

            return query;
        }
    }
}