// Copyright (c) 2009 Kasimier Buchcik
using System;

namespace Casimodo.Lib
{
    public delegate void CancelableEventHandler(object sender, CancelableEventArgs e);

    public class CancelableEventArgs : EventArgs
    {
        bool _isCancelled;
        bool _isCancelable;

        public CancelableEventArgs(bool isCancelable = true)
        {
            this._isCancelable = isCancelable;
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
    }
}