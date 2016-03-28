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
    public abstract partial class ObservableObject
    {
        /// <summary>
        /// Note that you'll get boxing if your ValueType noes not implement IEquatable.
        /// </summary>
        protected bool SetValueTypeProperty<T>(ObservablePropertyMetadata propMetadata, ref T oldValue, T newValue)
            where T : struct
        {
            return SetProperty<T>(propMetadata.Name, ref oldValue, ref newValue);
        }

        /// <summary>
        /// Note that you'll get boxing if your ValueType noes not implement IEquatable.
        /// </summary>
        public bool SetValueTypeProperty<T>(ObservablePropertyMetadata propMetadata, ref T oldValue, ref T newValue)
            where T : struct
        {
            return SetProperty<T>(propMetadata.Name, ref oldValue, ref newValue);
        }

        /// <summary>
        /// Note that you'll get boxing if your ValueType noes not implement IEquatable.
        /// </summary>
        protected bool SetValueTypeProperty<T>(PropertyChangedEventArgs propArgs, ref T oldValue, T newValue)
            where T : struct
        {
            return SetProperty<T>(propArgs.PropertyName, ref oldValue, ref newValue);
        }

        /// <summary>
        /// Note that you'll get boxing if your ValueType noes not implement IEquatable.
        /// </summary>
        protected bool SetValueTypeProperty<T>(string propertyName, ref T oldValue, T newValue)
            where T : struct
        {
            return SetProperty<T>(propertyName, ref oldValue, ref newValue);
        }

        public bool SetProperty<T>(ObservablePropertyMetadata propMetadata, T oldValue, T newValue, Action setter)
        {
            VerifyProperty(propMetadata.Name);

            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
            {
                // Deserialization behavior.
                setter();
                return false;
            }

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return false;

            setter();

            RaisePropertyChanged(propMetadata.Name);
            return true;
        }

        /// <summary>
        /// Note that you'll get boxing if your ValueType noes not implement IEquatable.
        /// </summary>
        protected bool SetProperty<T>(string propertyName, ref T oldValue, ref T newValue)
            where T : struct
        {
            VerifyProperty(propertyName);

            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
            {
                // Deserialization behavior.
                oldValue = newValue;
                return false;
            }

            if (oldValue is IEquatable<T>)
            {
                if (((IEquatable<T>)oldValue).Equals(newValue))
                    return false;
            }
            else
            {
                // Object.Equals(object) produces boxing.
                if (oldValue.Equals(newValue))
                    return false;
            }

            oldValue = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(ObservablePropertyMetadata propMetadata, ref Nullable<T> oldValue, Nullable<T> newValue)
           where T : struct
        {
            return SetProperty(propMetadata.Name, ref oldValue, newValue);
        }

        public bool SetProperty<T>(string propertyName, ref Nullable<T> oldValue, Nullable<T> newValue)
            where T : struct
        {
            VerifyProperty(propertyName);

            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
            {
                // Deserialization behavior.
                oldValue = newValue;
                return false;
            }

            if (Nullable.Equals<T>(oldValue, newValue))
                return false;

            oldValue = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(ObservablePropertyMetadata propMetadata, ref T oldValue, T newValue)
             where T : class
        {
            VerifyProperty(propMetadata.Name);

            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
            {
                // Deserialization behavior.
                oldValue = newValue;
                return false;
            }

            if (object.Equals(oldValue, newValue))
                return false;

            oldValue = newValue;
            RaisePropertyChanged(propMetadata.ChangedArgs);
            return true;
        }

        /// <summary>
        /// Supports null objects.
        /// If the new value is equal to the null object, then default(T) is assigned.
        /// </summary>        
        protected bool SetProperty<T>(ObservablePropertyMetadata propMetadata, ref T oldValue, T newValue, T nullObject)
            where T : class
        {
            VerifyProperty(propMetadata.Name);

            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
            {
                // Deserialization behavior.
                oldValue = newValue;
                return false;
            }

            if (object.Equals(newValue, nullObject))
                newValue = default(T);

            if (object.Equals(oldValue, newValue))
                return false;

            oldValue = newValue;
            RaisePropertyChanged(propMetadata.ChangedArgs);
            return true;
        }
    }
}
