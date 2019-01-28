using Casimodo.Lib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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

    public class TemplateLoopCursorVariable<T> : TemplateLoopCursor
      where T : class
    {
        public T Value { get { return (T)ValueObject; } }
    }

    public class TemplateLoopCursor
    {
        public object ValueObject { get; set; }
        public int Index { get; set; }
        public bool IsLast { get; set; }
        public bool IsFirst { get; set; }
        public bool IsOdd { get; set; }
        public int Count { get; set; }
    }


    public abstract class TemplateProcessor : ITemplateProcessor
    {
        public abstract void SetText(string value);
        public abstract void SetImage(Guid? imageFileId, bool removeIfEmpty = false);

        public abstract void RemoveValue();

        public CultureInfo Culture { get; protected set; } = CultureInfo.CurrentUICulture;

        public TemplateElement CurTemplateElement { get; protected set; }

        public bool IsMatch { get; set; }

        public bool IsErrorOnUnresolvedItemSupressed { get; set; }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public TemplateCoreContext CoreContext { get; set; }

        protected async Task<IEnumerable<object>> FindObjects(TemplateExpression expression)
        {
            return await GetExpressionProcessor().FindObjects(CoreContext, expression);
        }

        protected async Task<bool> EvaluateCondition(TemplateExpression expression)
        {
            return await GetExpressionProcessor().EvaluateCondition(CoreContext, expression);
        }

        protected async Task<object> EvaluateValue(TemplateExpression expression)
        {
            return await GetExpressionProcessor().EvaluateValue(CoreContext, expression);
        }

        bool _isInTransformation;

        public event TemplateProcessorEvent ElementExecuted;

        protected async Task ExecuteCurrentTemplateElement()
        {
            if (_isInTransformation)
                throw new TemplateException(
                    "A transformation is already being preformed. " +
                    "Nested transformations are not supported.");

            _isInTransformation = true;

            await Execute(CurTemplateElement);

            ElementExecuted?.Invoke(this, new TemplateProcessorEventArgs { Processor = this });

            if (!IsErrorOnUnresolvedItemSupressed)
                ThrowUnhandledTemplateIfNoMatch();

            _isInTransformation = false;
        }

        public async Task Execute(TemplateElement element)
        {
            var context = CoreContext.CreateExpressionContext(templateProcessor: this);

            context.Ast = ParseExpression(element);

            await GetExpressionProcessor().ExecuteAsync(context);

            // NOTE: Set value only if it was provided, because instructions might
            //   not return any value but manipulate the output directly instead.
            if (context.HasReturnValue)
                SetText(context.ReturnValue.FirstOrDefault());

            this.IsMatch = true;
        }

        public AstNode ParseExpression(TemplateExpression element)
        {
            return CoreContext.GetExpressionParser().ParseTemplateExpression(CoreContext.Data, element.Expression, element.Kind);
        }

        TemplateExpressionProcessor _pathProcessor;
        public TemplateExpressionProcessor GetExpressionProcessor()
        {
            if (_pathProcessor == null)
                _pathProcessor = new TemplateExpressionProcessor();

            return _pathProcessor;
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

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

        protected void ThrowUnhandledTemplateExpression(string expression)
        {
            throw new TemplateException($"Unhandled template element. Expression '{expression}'.");
        }

        public void ThrowUnhandledTemplateIfNoMatch()
        {
            if (!IsMatch)
                ThrowUnhandledTemplateExpression(CurTemplateElement.Expression);
        }
    }
}
