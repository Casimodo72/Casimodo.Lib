using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
#nullable enable

namespace Casimodo.Lib.Templates
{
    public abstract class TemplateProcessor : ITemplateProcessor
    {
        protected TemplateProcessor(TemplateContext context)
        {
            Guard.ArgNotNull(context, nameof(context));

            Context = context;
        }

        private TemplateContext Context { get; }

        public abstract void SetText(string value);
        public abstract void SetImage(Guid? imageFileId, bool removeIfEmpty = false);

        public abstract void RemoveValue();

        public CultureInfo Culture { get; protected set; } = CultureInfo.CurrentUICulture;

        public TemplateElement CurrentTemplateElement { get; protected set; }

        public bool IsMatch { get; set; }

        public bool IsErrorOnUnresolvedItemSupressed { get; set; }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public TemplateContext CoreContext { get; set; }

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

            await Execute(CurrentTemplateElement);

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
            _pathProcessor ??= new TemplateExpressionProcessor(Context);

            return _pathProcessor;
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool MatchesExpression(string expression)
        {
            if (IsMatch)
                return false;

            if (CurrentTemplateElement.Expression != expression)
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
            SetText(value?.ToString("d", Culture));
        }

        public void SetZonedTime(DateTimeOffset? value)
        {
            SetZonedDateTime(value, Culture.DateTimeFormat.ShortTimePattern);
        }

        public void SetZonedDateTime(DateTimeOffset? value, string format = null)
        {
            SetText(value.ToZonedString(format));
        }

        public static bool IsEmpty(string value)
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
            SetText(value?.ToString());
        }

        public void SetTextNonEmpty(string text)
        {
            if (!IsEmpty(text))
                SetText(text);
        }

        // TODO: Currently disabled
#if false
        public string GetDataUriFromEmbeddedPng(string name)
        {
            return TemplateUtils.ToDataUri("image/png", GetEmbeddedResource(name));
        }

        byte[] GetEmbeddedResource(string name)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ga.Web.Services.Assets." + name);
            using var ms = new System.IO.MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }  
#endif

        protected static void ThrowUnhandledTemplateExpression(string expression)
        {
            throw new TemplateException($"Unhandled template element. Expression '{expression}'.");
        }

        public void ThrowUnhandledTemplateIfNoMatch()
        {
            // TODO: Check if CurTemplateElement will be null if there's no template element.
            if (!IsMatch)
                ThrowUnhandledTemplateExpression(CurrentTemplateElement.Expression);
        }
    }
}
