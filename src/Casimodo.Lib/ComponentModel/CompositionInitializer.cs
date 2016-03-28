using System.ComponentModel.Composition.Hosting;

namespace System.ComponentModel.Composition
{
    public class CompositionInitializer
    {
        static CompositionInitializer _instance;

        static CompositionInitializer Instance
        {
            get { return _instance ?? (_instance = new CompositionInitializer()); }
        }

        CompositionContainer _container;

        CompositionInitializer()
        { }

        public static void SetContainer(CompositionContainer container)
        {
            Instance._container = container;
        }

        public static void SatisfyImports(object obj)
        {
            Instance._container.SatisfyImportsOnce(obj);
        }
    }
}