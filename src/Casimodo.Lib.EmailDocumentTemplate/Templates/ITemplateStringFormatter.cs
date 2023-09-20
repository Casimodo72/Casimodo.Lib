#nullable enable
using System;

namespace Casimodo.Lib.Templates
{
    public interface ITemplateStringFormatter
    {
        bool CanFormat(string format);
        string? Format(string? text, string format, IFormatProvider? formatProvider);
    }

    internal sealed class InternalTemplateStringFormatter : ITemplateStringFormatter
    {
        private readonly Func<string?, bool> _canFormat;
        private readonly Func<string?, string, IFormatProvider?, string?> _format;

        public InternalTemplateStringFormatter(Func<string?, bool> canFormat, Func<string?, string, IFormatProvider?, string?> format)
        {
            Guard.ArgNotNull(canFormat);
            Guard.ArgNotNull(format);

            _canFormat = canFormat;
            _format = format;
        }

        public bool CanFormat(string? format) => _canFormat(format);

        public string? Format(string? text, string format, IFormatProvider? formatProvider)
        {
            if (text == null) return null;

            return _format(text, format, formatProvider);
        }
    }
}
