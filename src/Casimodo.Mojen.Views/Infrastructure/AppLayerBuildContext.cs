using System.Collections.Generic;

namespace Casimodo.Lib.Mojen
{
    public class AppLayerBuildContext : MojenBuildContext
    {
        public AppLayerBuildContext()
        { }

        public MojViewBuilder AddSelector(MojType type)
        {
            var view = new MojViewConfig();
            view.RootView = view;
            view.TypeConfig = type;
            App.Add(view);

            var vbuilder = new MojViewBuilder(view);
            view.Template.ViewBuilder = vbuilder;

            return vbuilder;
        }

        public MojControllerBuilder AddController(MojType type)
        {
            var config = new MojControllerConfig(type.PluralName);
            Items.Add(config);

            var builder = new MojControllerBuilder(config);
            builder.Init(App);
            builder.Model(type);

            return builder;
        }

        public ItemBuilder Item(MojType type)
        {            
            var builder = new ItemBuilder(this, type);

            return builder;
        }

        public MiaTypeOperationsBuilder AddActions(MojType type)
        {
            type = type.StoreOrSelf;
            if (type.Kind != MojTypeKind.Entity)
                throw new MojenException("Data actions can be defined on entities only (i.e. not on models).");

            var builder = new MiaTypeOperationsBuilder { Type = type, App = App };

            return builder;
        }
    }
}