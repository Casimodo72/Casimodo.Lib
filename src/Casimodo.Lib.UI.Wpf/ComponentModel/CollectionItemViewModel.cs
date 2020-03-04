using Casimodo.Lib.ComponentModel;
using System.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    public abstract class CollectionItemViewModel : ObservableObject
    {
        // KABU TODO: REMOVE
        //protected static readonly PropertyChangedEventArgs IsSelectedChangedArgs = new PropertyChangedEventArgs("IsSelected");
        //protected static readonly PropertyChangedEventArgs IsCheckedChangedArgs = new PropertyChangedEventArgs("IsChecked");
        //protected static readonly PropertyChangedEventArgs IsEnabledChangedArgs = new PropertyChangedEventArgs("IsEnabled");

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProp(ref _isSelected, value); }
        }
        bool _isSelected;

        public bool IsChecked
        {
            get { return _IsChecked; }
            set { SetProp(ref _IsChecked, value); }
        }
        bool _IsChecked;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProp(ref _isEnabled, value); }
        }
        bool _isEnabled = true;
    }
}