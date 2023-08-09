namespace Casimodo.Mojen
{
    public abstract class DataLayerGenerator : MojenGenerator
    {
        public DataLayerGenerator()
        {
            ClassGen = AddSub<WebClassGen>();
            ClassGen.SetParent(this);
        }

        public WebClassGen ClassGen { get; private set; }

        public DataLayerConfig DataConfig { get; set; }

        public override void Prepare()
        {
            base.Prepare();
            DataConfig = App.Get<DataLayerConfig>();
        }

        public void OTsNamespace(string ns, Action action)
        {
            OB($"namespace {ns}");
            O();
            action?.Invoke();
            End();
        }

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
        }

        public void OTsClass(string name, string extends = null,
            IEnumerable<string> implements = null,
            bool isstatic = false, bool export = true,
            bool hasconstructor = true,
            bool propertyInitializer = false,
            Action constructor = null, Action content = null)
        {
            ClassGen.OTsClass(name, extends: extends, implements: implements,
                isstatic: isstatic, export: export,
                hasconstructor: hasconstructor,
                propertyInitializer: propertyInitializer,
                constructor: constructor, content: content);
        }

        public void OTsInterface(string name, string extends = null, bool export = true,
             Action content = null)
        {
            var isDerived = !string.IsNullOrEmpty(extends);

            OB($"{(export ? "export " : "")}interface {name}{(isDerived ? " extends " + extends : "")}");

            content.Invoke();

            End();
        }

        public string GetJsDefaultValue(MojProp prop)
        {
            if (prop.IsKey)
                return "cmodo.guid()"; // TODO: Make configurable

            if (prop.DefaultValues.Is)
            {
                var defaultValueConfig = prop.DefaultValues.ForScenario("OnEdit", null).FirstOrDefault();
                if (defaultValueConfig.Attr != null)
                {
                    return defaultValueConfig.Attr.Args.First().ToJsCodeString();
                }
                else if (defaultValueConfig.CommonValue != null)
                {
                    if (defaultValueConfig.CommonValue == MojDefaultValueCommon.CurrentYear)
                    {
                        return "new Date().getFullYear()";
                    }
                    else throw new MojenException($"Unhandled common default value kind '{defaultValueConfig.CommonValue}'.");
                }
                else if (defaultValueConfig.Value is string[])
                {
                    // Multiline text.
                    string multilineText = (defaultValueConfig.Value as string[]).Join("\\n");
                    return $"\"{multilineText}\"";
                }
                else
                {
                    return Moj.JS(defaultValueConfig.Value);
                }
            }

            return "null";
        }
    }
}