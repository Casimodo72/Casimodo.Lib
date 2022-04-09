namespace Casimodo.Mojen
{
    public class KendoWindowConfig
    {
        public const string DestructorFunction = "function() { this.destroy() }";

        public KendoWindowConfig()
        { }

        public KendoWindowConfig(MojViewConfig view)
        {
            Title = view.Title;
            Width = view.Width;
            MinWidth = view.MinWidth;
            MaxWidth = view.MaxWidth;
            MinHeight = view.MinHeight;
        }

        public KendoFxConfig Open { get; set; } = new KendoFxConfig(true)
        {
            Effects = "fadeIn", // "slideIn:down fadeIn",
            Duration = 400
        };

        public KendoFxConfig Close { get; set; } = new KendoFxConfig(true)
        {
            Effects = "fadeOut", // "slideIn:up fadeOut"
            Duration = 400
        };

        public string ContentUrl { get; set; }

        public string Title { get; set; }

        public object Animation { get; set; }

        public bool IsParentModal { get; set; }

        public bool IsModal { get; set; }

        public bool IsVisible { get; set; }

        public object OnClosing { get; set; }

        public object OnDeactivated { get; set; } = DestructorFunction;

        public int? Width { get; set; }

        public int? MinWidth { get; set; }

        public int? MaxWidth { get; set; }

        public int? MinHeight { get; set; }

        public int? Height { get; set; }
    }
}
