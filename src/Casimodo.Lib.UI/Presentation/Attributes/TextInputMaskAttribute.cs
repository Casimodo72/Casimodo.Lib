using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Casimodo.Lib.Presentation
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class TextInputMaskAttribute : Attribute
    {
        public TextInputMaskAttribute()
        { }

        public TextInputMaskAttribute(string mask)
        {
            this.Mask = mask;
        }

        public string Mask { get; set; }
    }
}