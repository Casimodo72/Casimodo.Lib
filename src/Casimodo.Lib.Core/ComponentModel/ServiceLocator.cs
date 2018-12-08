using System;

namespace Casimodo.Lib.ComponentModel
{
    /// <summary>
    /// ServiceLocator shim for .NET Core.
    /// Source: https://dotnetcoretutorials.com/2018/05/06/servicelocator-shim-for-net-core/
    /// </summary>
    public class ServiceLocator
    {
        private IServiceProvider _currentServiceProvider;
        private static IServiceProvider _serviceProvider;

        public ServiceLocator(IServiceProvider currentServiceProvider)
        {
            _currentServiceProvider = currentServiceProvider;
        }

        public static ServiceLocator Current
        {
            get { return new ServiceLocator(_serviceProvider); }
        }

        public static void SetLocatorProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TService GetInstance<TService>()
        {
            return (TService)GetInstance(typeof(TService));
        }

        public object GetInstance(Type serviceType)
        {
            return _currentServiceProvider.GetService(serviceType);
        }
    }
}
