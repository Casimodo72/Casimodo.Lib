// Copyright (c) 2010 Kasimier Buchcik
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Linq.Expressions;

namespace Casimodo.Lib.ComponentModel
{
    // KABU TODO: Maybe REMOVE when we can use nameof(abc.xyz) of C#6.
    public class ObservablePropertyMetadata
    {
        internal ObservablePropertyMetadata(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException("propertyName");

            propertyName = string.Intern(propertyName);
            Name = propertyName;
            ChangedArgs = new PropertyChangedEventArgs(propertyName);
        }

        public string Name { get; private set; }
        public PropertyChangedEventArgs ChangedArgs { get; private set; }

        public static ObservablePropertyMetadata Create<TType, TProp>(Expression<Func<TType, TProp>> propertyExpression)
        {
            return new ObservablePropertyMetadata(HProp.Name<TType, TProp>(propertyExpression));
        }

        public static ObservablePropertyMetadata Create(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException("propertyName");

            return new ObservablePropertyMetadata(propertyName);
        }
    }
}
