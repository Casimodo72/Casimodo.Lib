﻿// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.Prism;

namespace Casimodo.Lib.ComponentModel
{
    public class DelegateCommand<T> : XDelegateCommand<T>, ICommand, ICommandEx, INotifyPropertyChanged
    {
        public DelegateCommand(Action<T> executeMethod)
            : base(executeMethod)
        { }

        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
            : base(executeMethod, canExecuteMethod)
        { }

        public event EventHandler Executed;

        public override void Execute(T parameter)
        {
            base.Execute(parameter);

            var handler = Executed;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public string Text
        {
            get { return _text; }
            set
            {
                if (string.Equals(_text, value, StringComparison.Ordinal))
                    return;

                _text = value;

                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs("Text"));
            }
        }
        string _text;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
