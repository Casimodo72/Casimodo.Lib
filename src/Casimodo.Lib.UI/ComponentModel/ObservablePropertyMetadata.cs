// Copyright (c) 2010 Kasimier Buchcik
using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Casimodo.Lib.ComponentModel
{
    // KABU TODO: ELIMINATE
    public class ObservablePropertyMetadata
    {
        internal ObservablePropertyMetadata(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException("propertyName");

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