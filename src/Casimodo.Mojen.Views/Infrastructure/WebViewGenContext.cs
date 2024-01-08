namespace Casimodo.Mojen
{
    public class WebViewGenControlActionContext
    {
        public int ControlIndex { get; set; }
        public string CurrentId { get; set; }
    }

    public class WebViewGenContext
    {
        readonly Dictionary<string, object> _properties = [];

        public void SetProperty(string name, object value)
        {
            _properties[name] = value;
        }

        public T GetProperty<T>(string name)
        {
            return (T)_properties[name];
        }

        public string UINamespace { get; set; }

        public string ComponentId { get; set; }

        public string ComponentName { get; set; }

        public string ViewModelFactoryName { get; set; }
        public string ViewModelFactoryFullName { get; set; }

        public MojViewConfig View { get; set; }

        public string ViewRole { get; set; }

        public bool IsViewIdEnabled { get; set; }

        public MojViewPropInfo PropInfo { get; set; }

        public ViewTemplateItem Cur { get; set; }

        public WebViewGenControlActionContext CurControlAction { get; set; } = new WebViewGenControlActionContext();

        public List<ViewTemplateItem> Run { get; set; }
        public List<ViewTemplateItem> RunProps { get; set; }

        public bool IsRunEditable
        {
            get { return RunProps.Any(x => x.Prop.IsEditable); }
        }

        /// <summary>
        /// True for editors.
        /// False for read-only views, since they will render the full formed property navigation path.
        /// </summary>
        public bool IsEditableView { get; set; }

        public bool IsModalView { get; set; }

        public bool IsElementHidden(MojViewMode hideMode)
        {
            if (IsEditableView && (hideMode.HasFlag(MojViewMode.Create) || hideMode.HasFlag(MojViewMode.Update)))
                return true;

            if (!IsEditableView && (hideMode.HasFlag(MojViewMode.Read)))
                return true;

            return false;
        }

#if (false)
        /// <summary>
        /// NOTE: StopOnLooseReferences will be false by default.
        /// </summary>
        public KendoWebViewGenContext CreateSubContext()
        {
            var context = new KendoWebViewGenContext();
            context.View = View;
            context.ViewPropInfo = ViewPropInfo;

            return context;
        }
#endif
    }
}
