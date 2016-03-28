﻿using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public abstract class ODataControllerPartGenBase : WebPartGenerator
    {
        public ODataControllerPartGenBase()
        {
            Scope = "App";
        }

        public string Name { get; set; }

        public WebODataBuildConfig ODataConfig { get; set; }

        public Func<MojProp, bool> SelectProp { get; set; } = (prop) => true;

        public Func<IEnumerable<ControllerConfig>, IEnumerable<ControllerConfig>> SelectControllers { get; set; } = (controllers) => controllers;

        protected override void GenerateCore()
        {
            ODataConfig = App.Get<WebODataBuildConfig>();

            string path = Path.Combine(ODataConfig.WebODataControllersDirPath, "ODataControllerPart." + Name + ".generated.cs");

            PerformWrite(path, () => OForAllTypes());
        }

        protected ControllerConfig[] GetControllers()
        {
            return SelectControllers(App.GetItems<ControllerConfig>().Where(x => x.Uses<ODataControllerGen>())).ToArray();
        }

        public virtual void OType(MojType type)
        {
            // NOP
        }

        public virtual void OForAllTypes()
        {
            OUsing(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Threading",
                "System.Threading.Tasks",
                "System.Net",
                "System.Web.Http",
                "System.Web.Http.Controllers",
                "System.Web.OData",
                "System.Web.OData.Query",
                "System.Web.OData.Routing",
                "System.Data",
                "Casimodo.Lib",
                "Casimodo.Lib.Data",
                "Casimodo.Lib.Web",
                GetAllDataNamespaces()
            );

            ONamespace(ODataConfig.WebODataServicesNamespace);

            foreach (var controller in GetControllers())
            {
                var type = controller.TypeConfig;
                O($"public partial class {this.GetODataControllerName(type)}");
                Begin();

                OType(type);

                End(); // Controller
                O();
            }

            End(); // Namespace
        }
    }
}