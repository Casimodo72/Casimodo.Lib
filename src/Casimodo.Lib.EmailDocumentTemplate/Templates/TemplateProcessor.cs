using Casimodo.Lib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Casimodo.Lib.Templates
{
    public class TemplateElement
    {
        public string Expression { get; set; }
        public string RootContextName { get; set; }
        public object RootContextItem { get; set; }
        public string CurrentPath { get; set; }
        public bool IsCSharpExpression { get; set; }

        public TemplateElemKind Kind { get; set; } = TemplateElemKind.Property;
    };

    public enum TemplateElemKind
    {
        Property,
        Area
    }

    public class TemplateProp
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public static class TemplateUtils
    {
        static readonly string[] UriSchemes = new[]
        {
            "http://", "https://"
        };

        public static string RemoveSchemeFromUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return null;

            foreach (var scheme in UriSchemes)
                if (uri.StartsWith(scheme))
                    return uri.Substring(scheme.Length);

            return uri;
        }
    }

    public abstract class TemplateProcessor : ITemplateProcessor
    {
        public List<string> ListValues { get; set; } = new List<string>();

        public abstract void SetText(string value);
        public abstract void SetImage(Guid? imageFileId, bool removeIfEmpty = false);

        public abstract void RemoveValue();

        public CultureInfo Culture { get; protected set; } = CultureInfo.CurrentUICulture;

        public TemplateElement CurTemplateElement { get; protected set; }

        public bool IsMatch { get; set; }

        /// <summary>
        /// Additional properties (and values) provided by external means. E.g. by the FlexEmailDocumentTemplate.
        /// </summary>
        public List<TemplateProp> ExternalProperties { get; private set; } = new List<TemplateProp>();

        public bool ContextMatches(string name)
        {
            if (string.IsNullOrEmpty(name) &&
                string.IsNullOrEmpty(CurTemplateElement.RootContextName))
                return true;

            return CurTemplateElement.RootContextName == name;
        }

        public bool ContextMatches(string name, Type type)
        {
            if (ContextItem.Type != null)
                return ContextItem.Type == type;

            if (string.IsNullOrEmpty(name) &&
                string.IsNullOrEmpty(CurTemplateElement.RootContextName))
                return true;

            return CurTemplateElement.RootContextName == name;
        }

        public bool ContextMatches(Type type)
        {
            if (ContextItem.Type != null)
                return ContextItem.Type == type;

            return true;
        }

        public void SetCurrentContext(string name, object item)
        {
            ClearCurrentContext();
            ContextItem.Name = name;
            ContextItem.Item = item;
            if (item != null)
                ContextItem.Type = item.GetType();
        }

        public void ClearCurrentContext()
        {
            ContextItem.Name = null;
            ContextItem.Item = null;
            ContextItem.Type = null;
        }

        public class ContextItemInfo
        {
            public string Name { get; set; }
            public object Item { get; set; }
            public Type Type { get; set; }
        }

        public ContextItemInfo ContextItem { get; private set; } = new ContextItemInfo();

        public bool Matches(string path)
        {
            if (IsMatch)
                return false;

            if (CurTemplateElement.CurrentPath != path)
                return false;

            IsMatch = true;

            return true;
        }

        public bool EnableArea(object value)
        {
            if (value == null || (value is string && IsEmpty(value as string)))
            {
                RemoveValue();
                return false;
            }

            return true;
        }

        public bool EnableValue(object value)
        {
            if (value == null || (value is string && IsEmpty(value as string)))
            {
                RemoveValue();
                return false;
            }

            return true;
        }

        public void EnableArea(bool enabled)
        {
            if (!enabled) RemoveValue();
        }

        public void SetDate(DateTimeOffset? value)
        {
            SetText(value.ToDateString(Culture.DateTimeFormat.ShortDatePattern));
        }

        public void SetZonedTime(DateTimeOffset? value)
        {
            SetZonedDateTime(value, Culture.DateTimeFormat.ShortTimePattern);
        }

        public void SetZonedDateTime(DateTimeOffset? value, string format = null)
        {
            SetText(value.ToZonedString(format));
        }

        public bool IsEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public void SetTextOrRemove(object value)
        {
            var valueStr = value != null ? value.ToString().Trim() : "";
            if (IsEmpty(valueStr))
            {
                RemoveValue();
                return;
            }

            SetText(valueStr);
        }

        public void SetText(object value)
        {
            SetText(value != null ? value.ToString() : null);
        }

        public void SetTextNonEmpty(string text)
        {
            if (!IsEmpty(text))
                SetText(text);
        }

        public string GetDataUriFromEmbeddedPng(string name)
        {
            return GetDataUri("image/png", GetEmbeddedResource(name));
        }

        public byte[] GetEmbeddedResource(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ga.Web.Services.Assets." + name))
            using (var ms = new System.IO.MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Returns a data URI with base64 encoded data.
        /// </summary>        
        public string GetDataUri(string mediaType, byte[] data)
        {
            return $"data:{mediaType};base64,{Convert.ToBase64String(data)}";
        }

        protected void BuildTemplateElement(TemplateElement item)
        {
            if (item.IsCSharpExpression)
                return;

            // NOTE: If in doubt then one can use ":" as the separator between context-name and path.
            var idx = item.Expression.IndexOf(":", 0);
            if (idx == -1)
                idx = item.Expression.IndexOf(".", 0);

            if (idx != -1)
            {
                item.RootContextName = item.Expression.Substring(0, idx);

                // Ensure "env" is always lower cased.
                if (item.RootContextName.ToLower() == "env")
                    item.RootContextName = "env";

                idx += 1;
                if (idx < item.Expression.Length)
                    item.CurrentPath = item.Expression.Substring(idx);
            }
            else
            {
                item.CurrentPath = item.Expression;
            }
        }

        protected void ThrowUnhandledTemplateId(string id)
        {
            throw new TemplateProcessorException($"Unhandled template element ID '{id}'.");
        }

        public void ThrowUnhandledTemplateIfNoMatch()
        {
            if (!IsMatch)
                ThrowUnhandledTemplateId(CurTemplateElement.Expression);
        }
    }
}
