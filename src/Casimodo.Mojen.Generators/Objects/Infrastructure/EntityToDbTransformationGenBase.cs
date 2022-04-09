namespace Casimodo.Mojen
{
    public abstract class EntityToDbTransformationGenBase : DataLayerGenerator
    {
        public EntityToDbTransformationGenBase()
        {
            Scope = "Context";
        }

        public EntityToDbTransformationOptions Options { get; set; }

        protected override void GenerateCore()
        {
            Options = App.Get<EntityToDbTransformationOptions>(required: false);

            if (Options == null || !Options.IsEnabled ||
                string.IsNullOrEmpty(Options.InputDirPath) ||
                string.IsNullOrEmpty(Options.DbConnectionString))
                return;

            GenerateImport();
        }

        public abstract void GenerateImport();
    }
}