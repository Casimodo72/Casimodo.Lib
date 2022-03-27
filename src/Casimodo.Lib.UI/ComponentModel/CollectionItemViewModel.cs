using Casimodo.Lib.ComponentModel;

namespace Casimodo.Lib.UI
{
    public abstract class CollectionItemViewModel : ObservableObject
    {
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProp(ref _isSelected, value);
        }
        bool _isSelected;

        public bool IsChecked
        {
            get => _IsChecked;
            set => SetProp(ref _IsChecked, value);
        }
        bool _IsChecked;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProp(ref _isEnabled, value);
        }
        bool _isEnabled = true;
    }
}