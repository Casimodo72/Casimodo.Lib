// Copyright (c) 2009 Kasimier Buchcik
using System;

namespace Casimodo.Lib
{
    public delegate void CancelableChangingEventHandler<T>(object sender, CancelableChangingEventArgs<T> e);

    public class CancelableChangingEventArgs<T> : EventArgs
    {
        bool _isCancelled;
        readonly bool _isCancelable;

        public CancelableChangingEventArgs(T oldValue, T newValue, bool isCancelable)
        {
            _isCancelable = isCancelable;
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Cancels the operation.
        /// </summary>
        /// <returns>true if the operation was cancelled; false otherwise.</returns>
        public bool Cancel()
        {
            if (!_isCancelable)
                return false;

            _isCancelled = true;

            return true;
        }

        /// <summary>
        /// Indicates whether the operation is cancelable.
        /// </summary>
        public bool IsCancelable => _isCancelable;

        /// <summary>
        /// Indicates whether the operation should be canceled.
        /// </summary>
        public bool IsCancelled => _isCancelled;

        public T OldValue { get; private set; }

        public T NewValue { get; private set; }
    }
}