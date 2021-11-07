// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Casimodo.Lib.ComponentModel
{
    public interface IDisposableEx : IDisposable
    {
        bool IsDisposed { get; }
    }

    // TODO: REVISIT: Do we need INotifyPropertyChanging?
    [DataContract]
    public abstract partial class ObservableObject : INotifyPropertyChanged, IDisposable, IDisposableEx
    {
        [Flags]
        protected enum InternalFlags
        {
            None = 0,
            IsDeserializing = 1
        }

        const string VerifyErrorMsg = "{0} is not a public property of type {1}.";

        protected InternalFlags _internalFlags;

        public event PropertyChangedEventHandler PropertyChanged;

        [OnDeserializing]
        void OnDeserializing(StreamingContext context)
        {
            _internalFlags |= InternalFlags.IsDeserializing;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            _internalFlags &= ~InternalFlags.IsDeserializing;
        }

        // TODO: ELIMINATE
        public void FirePropertyChanged(string propertyName)
        {
            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;
            RaisePropertyChanged(propertyName);
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;

            VerifyProperty(propertyName);
            OnPropertyChanged(propertyName);
            RaisePropertyChangedCore(propertyName);
        }

        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;

            Guard.ArgNotNull(args, nameof(args));

            VerifyProperty(args.PropertyName);
            OnPropertyChanged(args.PropertyName);
            RaisePropertyChangedCore(args);
        }

        protected void RaisePropertyChangedCore(string propertyName)
        {
            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;

            var handler = PropertyChanged;
            if (handler == null)
                return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void RaisePropertyChangedCore(PropertyChangedEventArgs args)
        {
            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;

            var handler = PropertyChanged;
            if (handler == null)
                return;

            handler(this, args);
        }

        /// <summary>
        /// This one's needed in order to raise changes for "Item[]", etc.
        /// </summary>
        /// <param name="args"></param>
        public void RaisePropertyChangedNoVerification(PropertyChangedEventArgs args)
        {
            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;

            Guard.ArgNotNull(args, nameof(args));

            OnPropertyChanged(args.PropertyName);

            var handler = PropertyChanged;
            if (handler == null)
                return;

            handler(this, args);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            // NOP
        }

        // KABU TODO: REMOVE
#if (false)
        public bool SetProperty<T>(ref T oldValue, T newValue, [CallerMemberName] string propertyName = null)
        {
            return SetProp<T>(ref oldValue, newValue, propertyName);
        }
#endif
        public bool SetProp<T>(ref T oldValue, T newValue, [CallerMemberName] string propertyName = null)
        {
            VerifyProperty(propertyName);

            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
            {
                // Deserialization behavior.
                oldValue = newValue;
                return false;
            }

            // See http://stackoverflow.com/questions/864243/how-do-i-compare-a-generic-type-to-its-default-value

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return false;

            oldValue = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }

        public bool SetProp<T>(ref T oldValue, T newValue, PropertyChangedEventArgs args)
        {
            VerifyProperty(args.PropertyName);

            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
            {
                // Deserialization behavior.
                oldValue = newValue;
                return false;
            }

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return false;

            oldValue = newValue;
            RaisePropertyChanged(args);
            return true;
        }

        // KABU TODO: REMOVE
#if (false)
        public bool SetProperty<T>(T oldValue, T newValue, Action setter, [CallerMemberName] string propertyName = null)
        {
            return SetProp<T>(oldValue, newValue, setter, propertyName);
        }
#endif

        public bool SetProp<T>(T oldValue, T newValue, Action setter, [CallerMemberName] string propertyName = null)
        {
            VerifyProperty(propertyName);

            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
            {
                // Deserialization behavior.
                setter();
                return false;
            }

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return false;

            setter();

            RaisePropertyChanged(propertyName);
            return true;
        }

        // KABU TODO: REMOVE
#if (false)
        protected bool SetProperty<T>(string propertyName, ref T oldValue, T newValue)
            where T : class
        {
            VerifyProperty(propertyName);

            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
            {
                // Deserialization behavior.
                oldValue = newValue;
                return false;
            }

            if (object.Equals(oldValue, newValue))
                return false;

            oldValue = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }
#endif

        // Dispose ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected void CheckNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Called just before the instance is disposed.
        /// </summary>
        protected virtual void OnDisposing()
        {
            // NOP.
        }

        protected virtual void OnDisposed()
        {
            // NOP.
        }

        protected bool IsDisposed { get; private set; }

        bool IDisposableEx.IsDisposed => IsDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            OnDisposing();

            IsDisposed = true;

            PropertyChanged = null;

            OnDisposed();
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [Conditional("DEBUG")]
        void VerifyProperty(string propertyName)
        {
            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;
            ThrowIfPropertyNotFound(GetType(), propertyName);
        }

        public static void ThrowIfPropertyNotFound(Type type, string propertyName)
        {
            Guard.ArgNotNull(type, nameof(type));
            Guard.ArgNotNullOrWhitespace(propertyName, nameof(propertyName));

            // Look for a public property with the specified name.
            if (type.GetProperty(propertyName) == null)
            {
                // The property could not be found.
                throw new Exception(string.Format(VerifyErrorMsg, propertyName, type.FullName));
            }
        }
    }
}