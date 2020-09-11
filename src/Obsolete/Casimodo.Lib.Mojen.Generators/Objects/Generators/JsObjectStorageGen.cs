using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // KABU TODO: REMOVE: Not used anymore
    public class JsObjectStorageGen : DataLayerGenerator
    {
        public JsObjectStorageGen()
        {
            Scope = "Context";
        }

        public string ScriptNs { get; set; }
        public string ModuleName { get; set; }
        public string ServiceName { get; set; }

        protected override void GenerateCore()
        {
            var ctx = App.Get<DataLayerConfig>();

            var outputDirPath = ctx.AngularDataStorageDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            ScriptNs = ctx.ScriptNamespace;
            ModuleName = ctx.AngularModuleName;
            UsingServices.Add("dbcore");

            var types = App.GetRepositoryableEntities().ToArray();

            PerformWrite(Path.Combine(outputDirPath, "storage.generated.js"), () =>
            {
                OUseStrict();

                foreach (var type in types)
                    GenerateStorage(type);
            });
        }

        public void GenerateStorage(MojType type)
        {
            ServiceName = "db" + type.Name;
            GenerateAngularService(() =>
            {
                O($"var _key = dbcore.ns + '{type.Name}';");
                O();
                O("self.getItems = function() {");
                O("    if (!_items || !_items.length) {");
                O("        _items = dbcore.get(_key) || [];");
                O("    }");
                O("    return _items;");
                O("};");
                O();
                O("self.updateItems = function(items, progress) {");
                O("    if (dbcore.updateDownloadedItemList(self.getItems(), items, progress)) {");
                O("        dbcore.set(_key, _items);");
                O($"        dbcore.set(_key + '.UpdatedOnInfo', new {ScriptNs}.UpdatedOnInfo(dbcore.getLatestModifiedOn(_items)));");
                O("    }");
                O("};");
                O();
                O("self.getUpdateInfo = function() {");
                O($"    return dbcore.get(_key + '.UpdatedOnInfo') || new {ScriptNs}.UpdatedOnInfo();");
                O("};");
                O();
                O("self.deleteItems = function (ids) {");
                O("    return self.removeSomeEntities(ids,");
                O("        self.getItems(),");
                O("        function(items) {");
                O("            _items = items;");
                O("            dbcore.set(_key, _items);");
                O("        });");
                O("};");
            });
        }

        public List<string> UsingServices { get; set; } = new List<string>();

        public void GenerateAngularService(Action content)
        {
            O();
            Oo($"angular.module('{ModuleName}').service('{ServiceName}', [");
            foreach (var service in UsingServices)
            {
                o($"'{service}', ");
            }
            Br();
            Oo("function(");
            int i = 0;
            foreach (var service in UsingServices)
            {
                if (i++ > 0) o(", ");
                o(service);
            }
            oB(")");
            O("var self = this;");

            content();

            End("]);");
        }
    }
}