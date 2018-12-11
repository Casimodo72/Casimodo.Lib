using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public class MojenGenerator : MojenGeneratorBase
    {
        public MojenGenerator()
        {
            Scope = "App";
        }

        public void Generate()
        {
            Prepare();
            GenerateCore();
        }

        public virtual void Prepare()
        {
            SubGens.ForEach(x => x.Prepare());
        }

        public virtual MojenGenerator Initialize(MojenApp app)
        {
            App = app;
            SubGens.ForEach(x => x.Initialize(app));
            return this;
        }

        public MojenApp App { get; private set; }

        public string Stage { get; set; }
        public string Scope { get; set; }

        public T AddSub<T>() where T : MojenGenerator, new()
        {
            var gen = new T();
            SubGens.Add(gen);
            return gen;
        }

        public List<MojenGenerator> SubGens { get; set; } = new List<MojenGenerator>();

        protected virtual void GenerateCore()
        {
            // NOP
        }

        public string BuildLinqOrderBy(IEnumerable<MojOrderConfig> orders)
        {
            var sb = new StringBuilder();
            bool isfirst = true;
            foreach (var order in orders.OrderBy(x => x.Index))
            {
                if (order.Direction == MojOrderDirection.Ascending)
                {
                    if (isfirst)
                        sb.Append(string.Format(".OrderBy(x => x.{0})", order.Name));
                    else
                        sb.Append(string.Format(".ThenBy(x => x.{0})", order.Name));
                }
                else
                {
                    if (isfirst)
                        sb.Append(string.Format(".OrderByDescending(x => x.{0})", order.Name));
                    else
                        sb.Append(string.Format(".ThenByDescending(x => x.{0})", order.Name));
                }

                isfirst = false;
            }

            return sb.ToString();
        }

        public void ODefaultValueAttribute(MojProp prop, params string[] scenarios)
        {
            var defaultValue = prop.DefaultValues.ForScenario(scenarios).WithAttr().FirstOrDefault();
            if (defaultValue != null)
                O(defaultValue.Attr.ToString());
        }

        public void ORequiredAttribute(MojProp prop, bool required = true)
        {
            if (required && prop.Rules.IsRequired)
                O("[Required]");
            else if (prop.Rules.IsLocallyRequired)
                O("[LocallyRequired]");
        }

        public string GetDbSequenceFunction(MojProp prop)
        {
            return string.Format("GetNextSequenceValue(\"{0}\")", prop.DbAnno.Sequence.Name);
        }

        public string GetRepositoryName(MojType type)
        {
            return type.PluralName + (type.Kind == MojTypeKind.Model ? "Model" : "") + "Repository";
        }

        public void oQuote(string text)
        {
            if (text.Contains("{")) o("$");
            o("\"" + text + "\"");
        }

        public string Quote(string text)
        {
            return $"\"{text}\"";
        }
    }
}