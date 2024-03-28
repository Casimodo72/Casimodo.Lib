using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Casimodo.Lib
{
    public static class XmlLinqExtensions
    {
        static readonly char[] _separator = [','];

        public static TEnum EnumAttr<TEnum>(this XElement elem, string name, bool optional = true)
            where TEnum : struct
        {
            var attr = elem.Attr(name, optional);
            if (attr == null)
                return default;

            return (TEnum)Enum.Parse(typeof(TEnum), (string)attr);
        }

        public static bool HasAttr(this XElement elem, string name)
        {
            return elem.Attribute(name) != null;
        }

        public static IEnumerable<string> Strings(this XElement parent, string name, bool optional = true)
        {
            var elem = parent.Elem(name, optional);
            if (elem == null || string.IsNullOrWhiteSpace(elem.Value))
                return [];

            return elem.Value.Split(_separator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
        }

        public static XElement Elem(this XElement parent, string name, bool optional = false)
        {
            var elem = parent.Element(name);
            if (elem == null && !optional)
                throw new CasimodoLibException($"The required child element '{name}' was not found on element '{parent.Name}'.");

            return elem;
        }

        public static XAttribute Attr(this XElement elem, string name, bool optional = false)
        {
            var attr = elem.Attribute(name);
            if (attr == null && !optional)
                throw new CasimodoLibException($"The required attribute '{name}' was not found on element '{elem.Name}'.");

            return attr;
        }

        public static string ContentToString(this XElement elem, SaveOptions options)
        {
            if (elem == null)
                return null;

            var sb = new StringBuilder();

            var settings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                NamespaceHandling = options.HasFlag(SaveOptions.OmitDuplicateNamespaces) ? NamespaceHandling.OmitDuplicates : NamespaceHandling.Default,
                Indent = !options.HasFlag(SaveOptions.DisableFormatting)
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                foreach (var child in elem.Nodes())
                    child.WriteTo(writer);

                writer.Flush();
            }

            return sb.ToString();
        }
    }
}
