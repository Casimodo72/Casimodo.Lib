// Copyright (c) 2009 Kasimier Buchcik
using System;

namespace Casimodo.Lib
{
    public delegate void CancelableChangingEventHandler<T>(object sender, CancelableChangingEventArgs<T> e);

    public class CancelableChangingEventArgs<T> : EventArgs
    {
        bool _isCancelled;
        bool _isCancelable;

        public CancelableChangingEventArgs(T oldValue, T newValue, bool isCancelable)
        {
            this._isCancelable = isCancelable;
            this.OldValue = OldValue;
            this.NewValue = NewValue;
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
        public bool IsCancelable
        {
            get { return _isCancelable; }
        }

        /// <summary>
        /// Indicates whether the operation should be canceled.
        /// </summary>
        public bool IsCancelled
        {
            get { return _isCancelled; }
        }

        public T OldValue { get; private set; }

        public T NewValue { get; private set; }
    }
}