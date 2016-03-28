using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class ItemBuilder
    {
        public ItemBuilder(MojType config)
        {
            Config = config;
        }

        public MojType Config { get; private set; }

        public ItemBuilder Use<T>(object args = null)
             where T : AppPartGenerator
        {
            var use = MojenBuildExtensions.Use<T>(Config.UsingGenerators, args);

            if (use.Type == typeof(ODataControllerGen))
            {
                // Add implitic OData configuration generator.
                var modelGens = Config.UsingGenerators;
                if (!modelGens.Any(x => x.Type == typeof(ODataConfigGen)))
                    modelGens.Add(new MojUsingGeneratorConfig { Type = typeof(ODataConfigGen) });
            }

            return this;
        }
    }
}