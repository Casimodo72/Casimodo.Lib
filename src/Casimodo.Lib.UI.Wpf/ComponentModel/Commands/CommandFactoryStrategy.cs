using Casimodo.Lib.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.UI.Wpf.ComponentModel.Commands
{
    public class CommandFactoryStrategy : ICommandFactoryStrategy
    {
        public ICommandEx Create<T>(Action<T> execute, Func<T, bool> canExecute)
        {
#if (RelayCommand)
            return new RelayCommand<T>(execute, (a) => canExecute(a));
#else
            return new DelegateCommand<T>(
                (a) =>
                {
                    try
                    {
                        execute(a);
                    }
                    catch (Exception ex)
                    {
                        var handler = ServiceLocator.Current.GetInstance<ICommandErrorHandler>();
                        if (handler != null)
                            handler.HandleError(ex);
                        else
                            throw;
                    }
                },
                (a) =>
                {
                    try
                    {
                        return canExecute(a);
                    }
                    catch (Exception ex)
                    {
                        var handler = ServiceLocator.Current.GetInstance<ICommandErrorHandler>();
                        if (handler != null)
                        {
                            handler.HandleError(ex);
                            return false;
                        }
                        else
                            throw;
                    }
                });
#endif
        }
    }
}
