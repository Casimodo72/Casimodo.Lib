// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;

namespace Casimodo.Lib.Presentation
{
    public class DependencyPropertyChangedHelper<T> : DependencyObject
    {
        public static readonly DependencyProperty ValueProperty;
        public event DependencyPropertyChangedEventHandler ValueChanged;

        static DependencyPropertyChangedHelper()
        {
            ValueProperty = DependencyProperty.Register("Value", typeof(T), typeof(DependencyPropertyChangedHelper<T>),
                new PropertyMetadata(new PropertyChangedCallback(Value_Changed)));
        }

        public DependencyPropertyChangedHelper(DependencyObject trackedObject, string propertyName)
        {
            Binding binding = new Binding(propertyName);
            binding.Source = trackedObject;
            binding.Mode = BindingMode.OneWay;
            System.Windows.Data.BindingOperations.SetBinding(this, ValueProperty, binding);
        }       

        private static void Value_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DependencyPropertyChangedHelper<T> me = d as DependencyPropertyChangedHelper<T>;
            me.OnValueChanged(e);
        }

        private void OnValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if (ValueChanged != null)
            {
                ValueChanged(this, e);
            }
        }
    }
}
