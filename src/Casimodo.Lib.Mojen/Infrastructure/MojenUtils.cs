using Casimodo.Lib.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public static class MojenUtils
    {
        public static bool AreSame(MojProp prop, MojProp prop2)
        {
            if (prop == prop2)
                return true;

            if (prop.DeclaringType == prop2.DeclaringType)
            {
                if (prop.Name == prop2.Name)
                {
                    if (!prop.FormedNavigationTo.Is &&
                        !prop2.FormedNavigationTo.Is &&
                        !prop.FormedNavigationFrom.Is &&
                        !prop2.FormedNavigationFrom.Is)
                    {
                        throw new MojenException($"Warning: Two properties are the same but are different instances ('{prop.DeclaringType.ClassName}.{prop.Name}').");
                    }

                    return true;
                }
            }

            return false;
        }

        public static Type CreateType(MojType type, MojProp[] props)
        {
            // Build type dynamically for e.g. entity framework queries.
            // See http://stackoverflow.com/questions/26749429/anonymous-type-result-from-sql-query-execution-entity-framework
            var builder = TypeBuilderHelper.CreateTypeBuilder("DynAssembly", "DynModule", type.ClassName);

            foreach (var prop in props)
                TypeBuilderHelper.CreateAutoImplementedProperty(builder, prop.Name, prop.Type.Type);

            Type entityType = builder.CreateType();

            return entityType;
        }

        public static string FirstCharToLower(string str)
        {
            if (string.IsNullOrWhiteSpace(str) || char.IsLower(str, 0))
                return str;

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static string FirstCharToUpper(string str)
        {
            if (string.IsNullOrWhiteSpace(str) || char.IsUpper(str, 0))
                return str;

            return char.ToUpperInvariant(str[0]) + str.Substring(1);
        }

        public static bool IsDateTimeOrOffset(Type type)
        {
            return type != null && (type == typeof(DateTimeOffset) || type == typeof(DateTime));
        }

        public static string CollapseWhitespace(string text)
        {
            var sb = new StringBuilder(text.Length);
            bool ws = false;
            foreach (char ch in text)
            {
                if (char.IsWhiteSpace(ch))
                {
                    ws = true;
                    continue;
                }

                if (ws)
                {
                    sb.Append(' ');
                    ws = false;
                }

                sb.Append(ch);
            }

            return sb.ToString();
        }

        public static string GetCsCast(MojProp prop)
        {
            if (prop.Type.IsEnum || prop.Type.TypeNormalized == typeof(decimal))
                return "(" + prop.Type.Name + ")";

            return "";
        }

        public static string ToXmlValue(object value)
        {
            if (value == null)
                return null;

            var type = value.GetType();

            if (type == typeof(string))
                return (string)value;
            else if (type == typeof(int))
                return XmlConvert.ToString((int)value);
            else if (type == typeof(Enum))
                // NOTE: Enums as int
                return ((int)value).ToString();
            else if (type == typeof(bool))
                return XmlConvert.ToString((bool)value);
            else if (type == typeof(decimal))
                return XmlConvert.ToString((decimal)value);
            else if (type == typeof(double))
                return XmlConvert.ToString((double)value);
            else if (type == typeof(DateTimeOffset))
                return XmlConvert.ToString((DateTimeOffset)value);
            else if (type == typeof(DateTime))
                // KABU TODO: IMPORTANT: Not sure which mode to use
                return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Local);
            else if (type == typeof(Guid) || type == typeof(Guid?))
                return value.ToString();

            return string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        public static string ToCsValue(object value, bool parse = true, bool verbatim = false)
        {
            if (value == null)
                return "null";

            return ToCsValue(value, value.GetType(), parse);
        }

        public static string ToCsValue(object value, Type type, bool parse = true, bool verbatim = false)
        {
            if (value == null)
                return "null";

            if (type == null)
                return string.Format(CultureInfo.InvariantCulture, "{0}", value);

            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string))
            {
                return (verbatim ? "@" : "") + "\"" + ((string)value).Replace("\"", @"\""") + "\"";
            }
            else if (type == typeof(Enum))
                return value.ToString();
            else if (type == typeof(bool))
                return XmlConvert.ToString((bool)value);
            else if (type == typeof(decimal))
                return XmlConvert.ToString((decimal)value);
            else if (type == typeof(double))
                return XmlConvert.ToString((double)value);
            else if (type == typeof(DateTimeOffset))
            {
                return parse
                    ? "DateTimeOffset.Parse(\"" + XmlConvert.ToString((DateTimeOffset)value) + "\")"
                    : "\"" + XmlConvert.ToString((DateTimeOffset)value) + "\"";
            }
            else if (type == typeof(DateTime))
            {
                // KABU TODO: IMPORTANT: Not sure which mode to use
                return parse
                    ? "DateTime.Parse(\"" + XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Local) + "\")"
                    : "\"" + XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Local) + "\"";
            }
            else if (type == typeof(Guid))
            {
                return parse
                    ? "Guid.Parse(\"" + value + "\")"
                    : "\"" + value + "\"";
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        public static string ToJsValue(object value, bool parse = true, bool verbatim = false, bool quote = true, bool nullIfEmptyString = false)
        {
            if (value == null)
                return "null";

            if (nullIfEmptyString && value is string && string.IsNullOrEmpty((string)value))
                return "null";

            return ToJsValue(value, value.GetType(), parse: parse, quote: quote);
        }

        public static string ToJsXAttrValue(object value)
        {
            if (value == null)
                return "null";

            return ToJsValueCore(value, value.GetType(), parse: false, verbatim: false, quote: false);
        }

        public static string ToJsValue(object value, Type type, bool parse = true, bool verbatim = false, bool quote = true)
        {
            return ToJsValueCore(value, type, parse, verbatim, quote: quote);
        }

        const string jsQuote = "\""; // "'"

        static string ToJsValueCore(object value, Type type, bool parse = true, bool verbatim = false, bool quote = true)
        {
            if (value == null)
                return "null";

            if (type == null)
                return string.Format(CultureInfo.InvariantCulture, "{0}", value);

            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string))
            {
                // KABU: TODO: How to handle varbatim strings in JS?
                if (quote)
                    return jsQuote + value + jsQuote;
                else
                    return (string)value;
            }
            else if (type == typeof(Enum))
            {
                if (quote)
                    return jsQuote + value + jsQuote;
                else
                    return value.ToString();
            }
            else if (type == typeof(bool))
                return XmlConvert.ToString((bool)value);
            else if (type == typeof(decimal))
                return XmlConvert.ToString((decimal)value);
            else if (type == typeof(double))
                return XmlConvert.ToString((double)value);
            else if (type == typeof(DateTimeOffset))
            {
                // KABU TODO: IMPORTANT: How to convert DateTimeOffset exactly? (might not be possible at all).
                var time = (DateTimeOffset)value;
                return $"new Date({time.Year}, {time.Month}, {time.Day}, {time.Hour}, {time.Minute}, {time.Second}, {time.Millisecond})";
            }
            else if (type == typeof(DateTime))
            {
                throw new NotSupportedException("Conversion of DateTime to JS is not supported yet.");
                // KABU TODO: IMPORTANT: How to convert DateTime exactly?
                var time = (DateTime)value;
                return $"new Date({time.Year}, {time.Month}, {time.Day}, {time.Hour}, {time.Minute}, {time.Second}, {time.Millisecond})";
            }
            else if (type == typeof(Guid))
            {
                if (quote)
                    return jsQuote + value + jsQuote;
                else
                    return value.ToString();
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        public static string ToJsType(MojPropType type)
        {
            string t = "string";
            if (type.IsEnum)
                // KABU REVISIT: Maybe Symbol some day. https://developer.mozilla.org/en-US/docs/Glossary/Symbol
                t = "string";
            else if (type.IsNumber)
                t = "number";
            else if (type.IsBoolean)
                t = "boolean";
            else if (type.IsAnyTime)
                t = "date";

            return t;
        }

        public static string GetDefaultConstructor(MojPropType type)
        {
            if (type.TypeNormalized != null && type.TypeNormalized.IsArray)
                return ToCsType(type.TypeNormalized.GetElementType()) + "[0]";
            else
                return type.NameNormalized + "()";
        }

        public static string ToCsType(Type type, bool nullable = false)
        {
            if (type == typeof(string))
                return "string";
            else if (type == typeof(object))
                return "object";

            if (TypeHelper.IsNullableType(type))
            {
                nullable = true;
                type = Nullable.GetUnderlyingType(type);
            }

            string result;

            if (type.IsArray)
            {
                result = ToCsType(type.GetElementType()) + "[]";
            }
            else if (type.IsGenericType)
            {
                result = type.Name.Substring(0, type.Name.IndexOf('`'));
                result = string.Format("{0}<{1}>", result,
                    type.GetGenericArguments()
                        .Select(x => ToCsType(x))
                        .Join(", "));
            }
            else if (type == typeof(int))
                result = "int";
            else if (type == typeof(bool))
                result = "bool";
            else if (type == typeof(byte))
                result = "byte";
            else if (type == typeof(decimal))
                result = "decimal";
            else if (type == typeof(double))
                result = "double";
            else if (type == typeof(float))
                result = "float";
            else
                result = type.Name;

            return result + (nullable ? "?" : "");
        }

        public static IEnumerable<XElement> ConvertCsvToXml(string filePath, Encoding encoding = null)
        {
            return new CsvToXmlConverter().ConvertCsvToXml(filePath, encoding);
        }

        /// <summary>
        /// KABU TODO: IMPORTANT: Fix bug: in separator in quotes or double quotation marks escaping.
        /// </summary>
        public class CsvToXmlConverter
        {
            readonly StringBuilder _sb = new StringBuilder();
            readonly List<string> _values = new List<string>();
            readonly char Separator = ';';
            readonly char QuotationMark = '"';
            int _rowIndex = -1;
            int _numCols;

            public bool Trim { get; set; }

            void Reset()
            {
                _rowIndex = -1;
                _numCols = 0;
            }

            public IEnumerable<XElement> ConvertCsvToXml(string filePath, Encoding encoding = null)
            {
                return ConvertCsvToXml(File.ReadAllLines(filePath, encoding ?? Encoding.UTF8));
            }

            public IEnumerable<XElement> ConvertCsvToXml(string[] lines)
            {
                if (lines == null || lines.Length <= 1)
                    yield break;

                Reset();

                var rows =
                    (from row in lines
                     select new
                     {
                         Cols = GetValues(row).Select((value, index) => new
                         {
                             Index = index,
                             Value = value
                         }).ToArray()
                     }).ToArray();

                // Ignore non-named columns.
                var names = rows[0].Cols.Where(x => !string.IsNullOrEmpty(x.Value)).ToArray();
                var indexes = names.Select(x => x.Index).ToList();

                foreach (var row in rows.Skip(1))
                {
                    yield return new XElement("Item",
                        // Ignore non-named columns.
                        row.Cols.Where(col => indexes.Contains(col.Index))
                            .Select(col => new XElement(names[col.Index].Value, col.Value)));
                }
            }

            List<string> GetValues(string row)
            {
                _rowIndex++;
                _sb.Length = 0;
                _values.Clear();

                char ch;
                bool inquote = false;
                int i = 0;
                while (i < row.Length)
                {
                    ch = row[i];

                    if (ch == QuotationMark)
                    {
                        if (inquote)
                        {
                            // Check for escaped quotation marks ("");
                            if (i + 1 < row.Length && row[i + 1] == QuotationMark)
                            {
                                // Unescape to single quotation mark.
                                _sb.Append(ch);
                                i++;
                                i++;
                                continue;
                            }

                            inquote = false;
                        }
                        else
                            inquote = true;
                    }
                    else if (ch == Separator && !inquote)
                        AddValue();
                    else
                        _sb.Append(ch);

                    i++;
                }

                if (_sb.Length != 0)
                    AddValue();

                // Fill missing trailing columns.
                while (_numCols != 0 && _numCols > _values.Count)
                    _values.Add("");

                if (_rowIndex == 0)
                {
                    // Header columns define the total number of columns.
                    _numCols = _values.Count;
                }

                return _values;
            }

            void AddValue()
            {
                if (Trim)
                    _values.Add(_sb.ToString().Trim());
                else
                    _values.Add(_sb.ToString());

                _sb.Length = 0;
            }
        }
    }
}