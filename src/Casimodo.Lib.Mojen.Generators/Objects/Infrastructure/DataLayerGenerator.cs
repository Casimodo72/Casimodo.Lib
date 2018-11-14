using System;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public abstract class DataLayerGenerator : MojenGenerator
    {
        public DataLayerGenerator()
        {
            ClassGen = AddSub<JsClassGen>();
            ClassGen.SetParent(this);
        }

        public JsClassGen ClassGen { get; private set; }


        public void OJsNamespace(string ns, Action action)
        {
            OUseStrict();
            OB($"(function({ns})");

            action();

            O();
            End($")(window.{ns} || (window.{ns} = {{}}));");
        }

        public void OUseStrict()
        {
            O("\"use strict\";");
        }

        public void OJsClass(string name, bool isstatic = false, bool export = true, Action constructor = null, Action content = null)
        {
            OJsClass(App.Get<WebDataLayerConfig>().ScriptNamespace, name,
                isstatic: isstatic, export: export,
                constructor: constructor, content: content);
        }

        public void OJsClass(string ns, string name, bool isstatic = false, bool export = true, Action constructor = null, Action content = null)
        {
            ClassGen.OJsClass(ns, name, isstatic: isstatic, export: export, constructor: constructor, content: content);

            // KABU TOOD: REMOVE
            //OB($"var {name} = (function ()");
            //OB($"function {name}()");

            //content();

            //End();

            //O($"return {name};");

            //End(")();");

            //if (isstatic)
            //    O("{0}.{1} = new {1}();", ns, name);
            //else
            //    O("{0}.{1} = {1};", ns, name);
        }

        public string GetJsDefaultValue(MojProp prop)
        {
            if (prop.IsKey)
                return "cmodo.guid()";

            if (prop.DefaultValues.Is)
            {
                var @default = prop.DefaultValues.ForScenario("OnEdit", null).FirstOrDefault();
                if (@default.Attr != null)
                {
                    return @default.Attr.Args.First().ToJsCodeString();
                }
                else if (@default.CommonValue != null)
                {
                    if (@default.CommonValue == MojDefaultValueCommon.CurrentYear)
                    {
                        return "new Date().getFullYear()";
                    }
                    else throw new MojenException($"Unhandled common default value kind '{@default.CommonValue}'.");
                }
                else if (@default.Value is string[])
                {
                    // Multiline text.
                    string multilineText = (@default.Value as string[]).Join("\\n");
                    return $"\"{multilineText}\"";
                }
                else
                {
                    return MojenUtils.ToJsValue(@default.Value);
                }
            }

            // KABU TODO: REMOVE
            //string defaultValue = null;
            //var defaultValueArg = prop.Attrs.GetDefaultValueArg();
            //if (defaultValueArg != null)
            //{
            //    defaultValue = defaultValueArg.ToCodeString(parse: false);
            //    if (defaultValue == "")
            //        defaultValue = "\"\"";
            //}

            return "null";
        }
    }
}