using System;
using Casimodo.Lib.ComponentModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Casimodo.Lib.Presentation
{
    public class VisualStateMachine : ObservableObject
    {
#if (SILVERLIGHT)
        Control _item;

        public VisualStateMachine(Control control)
        {
            if (control == null)
                throw new ArgumentNullException("control");

            this._control = control;
        }
#else
        FrameworkElement _item;

        public VisualStateMachine(FrameworkElement item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            this._item = item;
        }
#endif

        public void GoToState(string stateName, bool useTransitions)
        {
#if (SILVERLIGHT)
            VisualStateManager.GoToState(_item, stateName, useTransitions);
#else
            VisualStateManager.GoToElementState(_item, stateName, useTransitions);
#endif
            CurrentStateName = stateName;
        }

        public string CurrentStateName
        {
            get { return _currentStateName; }
            set { SetProperty(CurrentStateNameChangedArgs, ref _currentStateName, value); }
        }
        string _currentStateName;
        public static readonly ObservablePropertyMetadata CurrentStateNameChangedArgs = ObservablePropertyMetadata.Create("CurrentStateName");
    }
}