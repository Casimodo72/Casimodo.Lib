//#define RelayCommand
//using Microsoft.Practices.ServiceLocation;
using System;

namespace Casimodo.Lib.ComponentModel
{
    public interface ICommandFactoryStrategy
    {
        ICommandEx Create<T>(Action<T> execute, Func<T, bool> canExecute);
    }

    public static class CommandFactory
    {
        // TODO: ELIMINATE
        public static ICommandFactoryStrategy Strategy;

        public static ICommandEx Create<T>(Action<T> execute, Func<T, bool> canExecute)
        {
            if (Strategy == null)
                throw new InvalidOperationException($"{nameof(Strategy)} not assigned.");

            return Strategy.Create(execute, canExecute);
        }

        public static ICommandEx Create<T>(Action<T> execute)
        {
            return Create<T>(execute, (a) => true);
        }

        public static ICommandEx Create(Action execute, Func<bool> canExecute)
        {
            return Create<object>((a) => execute(), (a) => canExecute());
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