using System;

namespace Casimodo.Lib.Templates
{
    public interface ITemplateProcessor
    {
        bool MatchesExpression(string expression);
        void SetText(object value);
        void SetText(string value);
        void SetTextOrRemove(object value);
        void SetTextNonEmpty(string text);
        void SetDate(DateTimeOffset? value);
        void SetZonedTime(DateTimeOffset? value);
        void SetZonedDateTime(DateTimeOffset? value, string format = null);
        bool EnableArea(object value);
        void EnableArea(bool enabled);
    }
}
