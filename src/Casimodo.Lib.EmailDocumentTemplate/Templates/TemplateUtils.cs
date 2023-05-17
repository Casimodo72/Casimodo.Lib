using System;
#nullable enable

namespace Casimodo.Lib.Templates
{
    public static class TemplateUtils
    {
        static readonly string[] UriSchemes = new[]
        {
            "http://", "https://"
        };

        public static string? RemoveSchemeFromUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return null;

            foreach (var scheme in UriSchemes)
                if (uri.StartsWith(scheme))
                    return uri[scheme.Length..];

            return uri;
        }

        /// <summary>
        /// Returns a data URI with base64 encoded data.
        /// </summary>        
        public static string ToDataUri(string mediaType, byte[] data)
        {
            return $"data:{mediaType};base64,{Convert.ToBase64String(data)}";
        }
    }
}
