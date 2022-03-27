using System;
using System.Windows.Input;

namespace Casimodo.Lib.ComponentModel
{
    public interface ICommandEx : ICommand
    {
        event EventHandler Executed;

        void RaiseCanExecuteChanged();

        bool CanExecute();

        void Execute();

        string Text { get; set; }

        public void Dispose();

#if (SILVERLIGHT && DEBUG)
        /// <summary>
        /// Returns the number of currently referenced handlers associated with the
        /// CanExecuteChanged event in order to debug memory leaks.
        /// </summary>
        int GetNumberOfHandlers();
#endif
    }
}