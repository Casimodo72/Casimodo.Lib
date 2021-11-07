// Copyright (c) 2009 Kasimier Buchcik
using System;

namespace Casimodo.Lib
{
    public delegate void CancelableEventHandler(object sender, CancelableEventArgs e);

    public class CancelableEventArgs : EventArgs
    {
        bool _isCancelled;
        readonly bool _isCancelable;

        public CancelableEventArgs(bool isCancelable = true)
        {
            _isCancelable = isCancelable;
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
    }
}