﻿using Casimodo.Lib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Casimodo.Lib.Templates
{
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
        public abstract void SetText(string value);
        public abstract void SetImage(Guid? imageFileId, bool removeIfEmpty = false);

        public abstract void RemoveValue();

        public CultureInfo Culture { get; protected set; } = CultureInfo.CurrentUICulture;

        public TemplateElement CurTemplateElement { get; protected set; }

        public bool IsMatch { get; set; }

        public bool MatchesExpression(string expression)
        {
            if (IsMatch)
                return false;

            if (CurTemplateElement.Expression != expression)
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

        public string ToDateString(DateTimeOffset? value)
        {
            return value?.ToDateString(Culture.DateTimeFormat.ShortDatePattern);
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
