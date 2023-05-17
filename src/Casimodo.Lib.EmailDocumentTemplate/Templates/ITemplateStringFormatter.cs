#nullable enable
using System;

namespace Casimodo.Lib.Templates
{
    internal sealed class InternalTemplateStringFormatter : ITemplateStringFormatter
    {
        private readonly Func<string, bool> _canFormat;
        private readonly Func<string, string, IFormatProvider?, string?> _format;

        public InternalTemplateStringFormatter(Func<string, bool> canFormat, Func<string?, string, IFormatProvider?, string?> format)
        {
            Guard.ArgNotNull(canFormat, nameof(canFormat));
            Guard.ArgNotNull(format, nameof(format));

            _canFormat = canFormat;
            _format = format;
        }

        public bool CanFormat(string format) => _canFormat(format);

        public string? Format(string? text, string format, IFormatProvider? formatProvider)
            => _format(text, format, formatProvider);
    }

    public interface ITemplateStringFormatter
    {
        bool CanFormat(string format);
        string? Format(string? text, string format, IFormatProvider? formatProvider);
    }
}
