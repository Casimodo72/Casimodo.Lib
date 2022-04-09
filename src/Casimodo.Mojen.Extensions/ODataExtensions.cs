namespace Casimodo.Mojen
{
    public static class ODataExtensions
    {
        public static MojControllerBuilder UseOData(this MojControllerBuilder builder, ODataControllerOptions options = null)
        {
            var type = builder.Controller.TypeConfig;
            // Add implitic OData configuration generator.
            var modelGens = type.UsingGenerators;
            if (!modelGens.Any(x => x.Type == typeof(ODataConfigGen)))
                modelGens.Add(new MojUsingGeneratorConfig { Type = typeof(ODataConfigGen) });

            builder.Use<CoreODataControllerGen>(options);

            return builder;
        }

        public static MojControllerBuilder UseODataMvcController(this MojControllerBuilder builder)
        {
            builder.Use<CoreODataMvcControllerGen>();

            return builder;
        }
    }
}