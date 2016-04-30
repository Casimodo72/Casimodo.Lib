using System;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public abstract class DataLayerGenerator : MojenGenerator
    {
        public void OJsNamespace(string ns, Action action)
        {
            OUseStrict();
            O($"var {ns};");
            OB($"(function({ns})");

            action();

            O();
            End($")({ns} || ({ns} = {{}}));");
        }

        public void OJsClass(string name, bool isstatic, Action content)
        {
            OB("var {0} = (function ()", name);
            OB("function {0}()", name);

            content();

            End();

            O("return {0};", name);

            End(")();");

            if (isstatic)
                O("{0}.{1} = new {1}();", App.Get<DataLayerConfig>().ScriptNamespace, name);
            else
                O("{0}.{1} = {1};", App.Get<DataLayerConfig>().ScriptNamespace, name);
        }

        public string GetJsDefaultValue(MojProp prop)
        {
            if (prop.IsKey)
                return "kendo.guid()";

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
                else throw new MojenException($"Unexpected default value object '{@default.Value}'.");
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