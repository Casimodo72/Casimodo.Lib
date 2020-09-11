using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public interface IMojenGenerateable
    {
        List<MojUsingGeneratorConfig> UsingGenerators { get; }
    }

    public static class MojenBuildExtensions
    {
        public static T GetGeneratorOptions<T>(this IMojenGenerateable generateable)
            where T : class
        {
            return generateable.UsingGenerators.Select(x => x.Args).FirstOrDefault(x => x != null && x is T) as T;
        }

        public static bool Uses(this IMojenGenerateable generateable, MojenGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException("generator");
            return generateable.UsingGenerators.Any(x => x.Type == generator.GetType());
        }

        public static MojUsingGeneratorConfig Use<TUse>(List<MojUsingGeneratorConfig> uses, object args)
            where TUse : class
        {
            Type type = typeof(TUse);
            var use = uses.FirstOrDefault(x => x.Type == type);

            if (use == null)
            {
                use = new MojUsingGeneratorConfig { Type = type };
                uses.Add(use);
            }

            use.AddArgs(args);

            return use;
        }
    }

    public class MojenBuilder
    {
        public MojenApp App { get; set; }

        internal protected virtual List<MojUsingGeneratorConfig> UsingGenerators
        {
            get { return null; }
        }

        public bool Uses(MojenGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException("generator");
            return UsingGenerators != null && UsingGenerators.Any(x => x.Type == generator.GetType());
        }
    }

    public class MojenBuilder<TBuilder> : MojenBuilder
        where TBuilder : MojenBuilder<TBuilder>
    {
        public TBuilder Use<TGenerator>(object args = null)
            where TGenerator : MojenGenerator
        {
            MojenBuildExtensions.Use<TGenerator>(UsingGenerators, args);

            return This();
        }

        protected TBuilder This()
        {
            return (TBuilder)this;
        }
    }
}
