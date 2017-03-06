using System;
using System.Windows.Input;

namespace Casimodo.Lib.ComponentModel
{
    public interface ICommandEx : ICommand
    {
        event EventHandler Executed;

        void RaiseCanExecuteChanged();

        string Text { get; set; }

#if (SILVERLIGHT && DEBUG)
        /// <summary>
        /// Returns the number of currently referenced handlers associated with the
        /// CanExecuteChanged event in order to debug memory leaks.
        /// </summary>
        int GetNumberOfHandlers();
#endif
    }
}