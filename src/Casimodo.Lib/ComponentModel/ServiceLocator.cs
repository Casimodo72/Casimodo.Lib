using System;

namespace Casimodo.Lib.ComponentModel
{
    /// <summary>
    /// ServiceLocator shim for .NET Core.
    /// Source: https://dotnetcoretutorials.com/2018/05/06/servicelocator-shim-for-net-core/
    /// </summary>
    public class ServiceLocator
    {
        private readonly IServiceProvider _currentServiceProvider;
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

        public TService GetRequiredInstance<TService>()
        {
            return (TService)GetRequiredInstance(typeof(TService));
        }

        public object GetInstance(Type serviceType)
        {
            return _currentServiceProvider.GetService(serviceType);
        }

        public object GetRequiredInstance(Type serviceType)
        {
            var instance = GetInstance(serviceType);
            if (instance == null)
            {
                throw new Exception($"ServiceLocator: Failed to find an instance of '{serviceType.Name}'.");
            }

            return instance;
        }
    }
}
