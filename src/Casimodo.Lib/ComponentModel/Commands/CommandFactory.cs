//#define RelayCommand
using CommonServiceLocator;
using System;

namespace Casimodo.Lib.ComponentModel
{
    public static class CommandFactory
    {
        public static ICommandEx Create<T>(Action<T> execute, Func<T, bool> canExecute)
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

        public static ICommandEx Create<T>(Action<T> execute)
        {
            return Create<T>(execute, (a) => true);
            // KABU TODO: REMOVE?
            //#if (RelayCommand)
            //            return new RelayCommand<T>(execute, (a) => true);
            //#else
            //            return new DelegateCommand<T>(execute, (a) => true);
            //#endif
        }

        public static ICommandEx Create(Action execute, Func<bool> canExecute)
        {
            return Create<object>((a) => execute(), (a) => canExecute());
            // KABU TODO: REMOVE?
            //#if (RelayCommand)
            //            return new RelayCommand(execute, canExecute);
            //#else
            //            return new DelegateCommand<object>((a) => execute(), (a) => canExecute());
            //#endif
        }

        public static ICommandEx Create(Action execute)
        {
            return Create(execute, () => true);
        }

        public static ICommandEx Get<T>(ref ICommandEx command, Action<T> execute, Func<T, bool> canExecute)
        {
            if (command == null)
                command = Create(execute, canExecute);

            return command;
        }

        public static ICommandEx Get<T>(ref ICommandEx command, Action<T> execute)
        {
            if (command == null)
                command = Create(execute);

            return command;
        }

        public static ICommandEx Get(ref ICommandEx command, Action execute, Func<bool> canExecute)
        {
            if (command == null)
                command = Create(execute, canExecute);

            return command;
        }

        public static ICommandEx Get(ref ICommandEx command, Action execute)
        {
            if (command == null)
                command = Create(execute);

            return command;
        }
    }
}