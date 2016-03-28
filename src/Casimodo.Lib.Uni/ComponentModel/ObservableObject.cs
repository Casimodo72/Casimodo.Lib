// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Casimodo.Lib.ComponentModel
{
    //public static class ObservableObjectExtensions
    //{
    //    public static bool IsDisposed(this ObservableObject item)
    //    {
    //        // NOTE: I moved the property "IsDisposed" into an extension method in order
    //        // to hide it from convention based machinery like EF CodeFirst (I would have
    //        // had to annotate it with attribute [NotMapped] otherwise).
    //        return item.IsDisposed;
    //    }
    //}

    public interface IDisposableEx : IDisposable
    {
        bool IsDisposed { get; }
    }

    // See http://stackoverflow.com/questions/864243/how-do-i-compare-a-generic-type-to-its-default-value

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
            _internalFlags = _internalFlags | InternalFlags.IsDeserializing;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            _internalFlags = _internalFlags & ~InternalFlags.IsDeserializing;
        }

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
            if (args == null)
                throw new ArgumentNullException("args");

            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;

            VerifyProperty(args.PropertyName);
            OnPropertyChanged(args.PropertyName);
            RaisePropertyChangedCore(args.PropertyName);
        }

        protected void RaisePropertyChanged(ObservablePropertyMetadata property)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;

            VerifyProperty(property.Name);
            OnPropertyChanged(property.Name);
            RaisePropertyChangedCore(property.Name);
        }

        /// <summary>
        /// This one's needed in order to raise changes for "Item[]", etc.
        /// </summary>
        /// <param name="args"></param>
        public void RaisePropertyChangedNoVerification(PropertyChangedEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
                return;

            OnPropertyChanged(args.PropertyName);

            var handler = PropertyChanged;
            if (handler == null)
                return;

            handler(this, args);
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            // NOP
        }

        public bool SetProperty<T>(ref T oldValue, T newValue, [CallerMemberName] string propertyName = null)
        {
            return SetProp<T>(ref oldValue, newValue, propertyName);
        }

        public bool SetProp<T>(ref T oldValue, T newValue, [CallerMemberName] string propertyName = null)
        {
            VerifyProperty(propertyName);

            if (_internalFlags.HasFlag(InternalFlags.IsDeserializing))
            {
                // Deserialization behavior.            
                return false;
            }

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return false;

            oldValue = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }

        public bool SetProperty<T>(T oldValue, T newValue, Action setter, [CallerMemberName] string propertyName = null)
        {
            return SetProp<T>(oldValue, newValue, setter, propertyName);
        }

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

        // KABU TODO: Still used?
        protected bool SetProperty<T>(string propertyName, ref T oldValue, T newValue)
            where T : class
        {
            VerifyProperty(propertyName);

            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
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

        // Dispose ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~              

        protected void CheckNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// Called just before the instance is disposed.
        /// </summary>
        protected virtual void OnDisposing()
        {
            // NOP.
        }

        protected virtual void OnDispose()
        {
            // NOP.
        }

        protected bool IsDisposed { get; private set; }

        bool IDisposableEx.IsDisposed
        {
            get { return IsDisposed; }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            OnDisposing();

            IsDisposed = true;

            PropertyChanged = null;

            OnDispose();
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [Conditional("DEBUG")]
        void VerifyProperty(string propertyName)
        {
            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
                return;
            ValidatePropertyExistance(this.GetType(), propertyName);
        }

        public static void ValidatePropertyExistance(Type type, string propertyName)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException("propertyName");

            // Look for a public property with the specified name.
            PropertyInfo propInfo = type.GetProperty(propertyName);

            if (propInfo == null)
            {
                // The property could not be found.
                throw new Exception(string.Format(VerifyErrorMsg, propertyName, type.FullName));
            }
        }
    }
}
