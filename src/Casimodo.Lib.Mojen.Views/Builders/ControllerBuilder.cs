using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class MojControllerBuilder
    {
        public MojControllerBuilder(MojControllerConfig config)
        {
            Controller = config;
            ViewBuilders = new List<MojViewBuilder>();
        }

        public MojControllerConfig Controller { get; private set; }

        public List<MojViewBuilder> ViewBuilders { get; private set; }

        public MojenApp App { get; set; }

        public void Init(MojenApp app)
        {
            App = app;
        }

        public MojControllerBuilder Model(MojType model)
        {
            Controller.TypeConfig = model;
            return this;
        }

        public void Add(MojViewConfig view)
        {
            Controller.Views.Add(view);
        }

        public MojViewBuilder DetailsView(string id)
        {
            // KABU TODO: Make detail views Partial() by default.
            return View(id).Details();
        }

        public MojViewBuilder PageView(string id)
        {
            return View(id).Page();
        }

        public MojViewBuilder ListView(string id)
        {
            return View(id).List();
        }

        public MojViewBuilder EditorView(string id, params MojProp[] parameters)
        {
            return View(id).Editor(parameters);
        }

        public MojViewBuilder ListDialog(string id)
        {
            return View(id).ListDialog()
                .CanEdit(false)
                .CanCreate(false)
                .CanDelete(false);
        }

        public MojViewBuilder SingleLookupView(string id, params MojProp[] parameters)
        {
            return View(id).SingleLookupView(parameters);
        }

        public MojViewBuilder View(string id = null)
        {
            Guard.ArgNotNullOrWhitespace(id, nameof(id));

            var view = new MojControllerViewConfig();
            view.Id = id;
            view.Controller = Controller;
            view.TypeConfig = Controller.TypeConfig;

            if (App.Items.Any(x => (x as MojViewConfig)?.Id == id))
                throw new MojenException($"Duplicate view ID '{id}'.");
            App.Add(view);

            var vbuilder = new MojControllerViewBuilder(this, view);
            view.Template.ViewBuilder = vbuilder;
            Controller.Views.Add(vbuilder.View);
            ViewBuilders.Add(vbuilder);

            return vbuilder;
        }

        public MojControllerBuilder ApiAuthRole(string role, string permit = "*") // , string deny = null)
        {
            Controller.AuthPermissions.Add(new MojAuthPermission
            {
                Role = role,
                Permit = permit,
                Deny = null // deny
            });

            return this;
        }

        public MojControllerBuilder Use<T>(object args = null)
            where T : MojenGenerator
        {
            var use = MojenBuildExtensions.Use<T>(Controller.UsingGenerators, args);

            // KABU TODO: REMOVE? Not used anymore. 
            //MojenApp.HandleUsingBy(new MojUsedByEventArgs
            //{
            //    UsedType = use.Type,
            //    UsedByObject = Controller.TypeConfig
            //});

            return this;
        }

        public MojControllerBuilder AddAttr(string name, params MojAttrArg[] args)
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