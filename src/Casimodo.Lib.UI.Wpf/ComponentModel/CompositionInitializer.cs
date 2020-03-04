using System.ComponentModel.Composition.Hosting;

namespace System.ComponentModel.Composition
{
    public class CompositionHost
    {

        internal static CompositionContainer _container = null;

        public static void Initialize(CompositionContainer container)
        {
            _container = container;
        }

        public static CompositionContainer CompositionInitializer
        {
            get { return _container as CompositionContainer; }
        }
    }

    public class CompositionInitializer
    {
        static CompositionInitializer _current;

        static CompositionInitializer Current
        {
            get { return _current ?? (_current = new CompositionInitializer()); }
        }

        CompositionContainer _container;

        CompositionInitializer()
        { }

        public static void SetContainer(CompositionContainer container)
        {
            Current._container = container;
        }

        public static void SatisfyImports(object obj)
        {
            Current._container.SatisfyImportsOnce(obj);
        }
    }
}