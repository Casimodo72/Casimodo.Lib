﻿using System;
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

        public MojViewBuilder IndexView(string id)
        {
            return View(id).Index();
        }

        public MojViewBuilder ListView(string id)
        {
            return View(id).List();
        }

        public MojViewBuilder EditorView(string id)
        {
            return View(id).Editor();
        }

        public MojViewBuilder StandaloneEditorView(string id)
        {
            return View(id).StandaloneEditorView()
                .CanEdit(true)
                .CanCreate(false)
                .CanDelete(false);
        }

        public MojViewBuilder StandaloneReadOnlyView(string id)
        {
            return View(id).StandaloneReadOnlyView();
        }

        public MojViewBuilder ListDialog(string id)
        {
            return View(id).ListDialog()
                .CanEdit(false)
                .CanCreate(false)
                .CanDelete(false);
        }

        public MojViewBuilder LookupSingleView(string id, params MojProp[] parameters)
        {
            return View().LookupSingle(id, parameters);
        }

        MojViewBuilder View(string id = null)
        {
            var view = new MojControllerViewConfig();
            view.Controller = Controller;
            view.TypeConfig = Controller.TypeConfig;
            App.Add(view);

            var vbuilder = new MojControllerViewBuilder(this, view);
            view.Template.ViewBuilder = vbuilder;
            Controller.Views.Add(vbuilder.View);
            ViewBuilders.Add(vbuilder);

            if (id != null)
                vbuilder.Id(id);

            return vbuilder;
        }

        public MojControllerBuilder Use<T>(object args = null)
            where T : MojenGenerator
        {
            var use = MojenBuildExtensions.Use<T>(Controller.UsingGenerators, args);

            MojenApp.HandleUsingBy(new MojUsedByEventArgs
            {
                UsedType = use.Type,
                UsedByObject = Controller.TypeConfig
            });

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