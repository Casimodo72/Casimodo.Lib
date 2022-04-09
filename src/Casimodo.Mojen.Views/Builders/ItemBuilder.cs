namespace Casimodo.Mojen
{
    public class ItemBuilder
    {
        public ItemBuilder(MojenBuildContext context, MojType config)
        {
            Context = context;
            Config = config;
        }

        public MojenBuildContext Context { get; set; }

        public MojType Config { get; private set; }

        public ItemBuilder Use<T>(object args = null)
             where T : AppPartGenerator
        {
            var use = MojenBuildExtensions.Use<T>(Config.UsingGenerators, args);

            // KABU TODO: REMOVE? Not used anymore. 
            //MojenApp.HandleUsingBy(new MojUsedByEventArgs
            //{
            //    UsedType = use.Type,
            //    UsedByObject = Config
            //});

            return this;
        }
    }
}