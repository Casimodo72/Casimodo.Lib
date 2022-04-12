#nullable enable
namespace Casimodo.Mojen.App.Generators.Blazor.Blazorise
{
    public static class BlazoriseHelper
    {
        public static MojViewBuilder BlazoriseFormEditor(this MojenApp app, string id, MojType type)
        {
            return CreateEditorBase(app, id, type)
                .CanCreate(true)
                .CanEdit(true)
                .CanDelete(true);
        }

        static MojViewBuilder CreateEditorBase(this MojenApp app, string id, MojType type)
        {
            return CreateViewBuilder(app, id, type)
                .Use<BlazoriseEditorFormGen>()
                .Editor()
                .CanEdit(false)
                .CanCreate(false)
                .CanDelete(false)
                //.Auth()
                ;
        }

        public static MojViewBuilder BlazoriseLookupSingle(this MojenApp app, string id, MojType type,
            // KendoGridOptions options = null,
            params MojProp[] parameters)
        {
            var builder = app.CreateLookupSingleView(id, type, parameters)
                .Use<BlazoriseLookupViewGen>() // options ?? new KendoGridOptions { PageSize = 10 })
                .Modal()
                .Selectable()
                //.Auth()
                ;

            // SetCustomKendoGridFilters(builder, globalCompanyFilter: false);

            return builder;
        }

        static MojViewBuilder CreateLookupSingleView(this MojenApp app, string id, MojType type, params MojProp[] parameters)
        {
            return app.CreateViewBuilder(id, type).SingleLookupView(parameters);
        }

        static MojViewBuilder CreateViewBuilder(this MojenApp app, string id, MojType type)
        {
            var view = new MojViewConfig
            {
                Id = id,
                TypeConfig = type
            };

            if (app.GetItems<MojViewConfig>().Any(view => view.Id == id))
            {
                throw new MojenException($"Duplicate view ID '{id}'.");
            }

            app.Add(view);

            var viewBuilder = new MojViewBuilder(view);

            view.Template.ViewBuilder = viewBuilder;

            return viewBuilder;
        }
    }
}
