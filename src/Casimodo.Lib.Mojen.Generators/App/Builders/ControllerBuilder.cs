using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class ControllerBuilder
    {
        public ControllerBuilder(ControllerConfig config)
        {
            Controller = config;
            ViewBuilders = new List<MojViewBuilder>();
        }

        public ControllerConfig Controller { get; private set; }

        public List<MojViewBuilder> ViewBuilders { get; private set; }

        public MojenApp App { get; set; }

        public void Init(MojenApp app)
        {
            App = app;
        }

        public ControllerBuilder Model(MojType model)
        {
            Controller.TypeConfig = model;
            return this;
        }

        public void Add(MojViewConfig view)
        {
            Controller.Views.Add(view);
        }

        public MojViewBuilder DetailsView()
        {
            return View().Details();
        }

        public MojViewBuilder IndexView()
        {
            return View().Index();
        }

        public MojViewBuilder ListView()
        {
            return View().List();
        }

        public MojViewBuilder EditorView()
        {
            return View().Editor();
        }

        public MojViewBuilder StandaloneEditorDialog(string id)
        {
            return View().StandaloneEditorDialog()
                .Id(id)
                .CanEdit(true)
                .CanCreate(false)
                .CanDelete(false);
        }

        public MojViewBuilder StandaloneDetailsView(string id)
        {
            return View().StandaloneDetailsView()
                .Id(id);
        }

        public MojViewBuilder ListDialog(string id)
        {
            return View().ListDialog()
                .Id(id)
                .CanEdit(false)
                .CanCreate(false)
                .CanDelete(false);
        }

        public MojViewBuilder LookupSingleView(params MojProp[] parameters)
        {
            return View().LookupSingle(parameters);
        }

        MojViewBuilder View()
        {
            var view = new MojControllerViewConfig();
            view.Controller = Controller;
            view.TypeConfig = Controller.TypeConfig;
            App.Add(view);

            var vbuilder = new MojControllerViewBuilder(this, view);
            view.Template.ViewBuilder = vbuilder;
            Controller.Views.Add(vbuilder.View);
            ViewBuilders.Add(vbuilder);

            return vbuilder;
        }

        public ControllerBuilder Use<T>(object args = null)
            where T : MojenGenerator
        {
            var use = MojenBuildExtensions.Use<T>(Controller.UsingGenerators, args);

            if (use.Type == typeof(ODataControllerGen))
            {
                // Add implitic OData configuration generator on the MojType.
                var modelGens = Controller.TypeConfig.UsingGenerators;
                if (!modelGens.Any(x => x.Type == typeof(ODataConfigGen)))
                    modelGens.Add(new MojUsingGeneratorConfig { Type = typeof(ODataConfigGen) });
            }

            return this;
        }

        public ControllerBuilder AddAttr(string name, params MojAttrArg[] args)
        {
            //Args.Add(new PropAttrArg { IsConstructorArg = isConstructor, Name = name, Value = value, IsString = isString, IsStringUnescaped = isStringUnescaped });
            var attr = new MojAttr(name, 1);
            if (args != null)
                attr.Args.AddRange(args);
            Controller.Attrs.Add(attr);

            return this;
        }

        public object On(object update)
        {
            throw new NotImplementedException();
        }
    }
}