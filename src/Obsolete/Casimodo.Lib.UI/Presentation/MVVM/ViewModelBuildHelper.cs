using System.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    public static class ViewModelHelper
    {
        public static TModel Initialize<TModel>(TModel model)
            where TModel : IViewModel
        {
            return Initialize<TModel>(model, null);
        }

        public static TModel Initialize<TModel>(TModel model, object args)
            where TModel : IViewModel
        {
            model.Initialize(args);          
            return model;
        }
    }
}