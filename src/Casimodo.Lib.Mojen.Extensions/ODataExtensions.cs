using System.Linq;

namespace Casimodo.Lib.Mojen
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

            if (builder.App.IsDotNetCore())
                builder.Use<CoreODataControllerGen>(options);
            else
                builder.Use<ODataControllerGen>(options);

            return builder;
        }
    }
}