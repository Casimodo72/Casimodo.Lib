using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#nullable enable

namespace Casimodo.Lib.Templates
{
    internal sealed class NoopTemplateContext : TemplateContext
    {
        public NoopTemplateContext()
            : base(new TemplateDataContainer(), CultureInfo.InvariantCulture)
        {
        }

        public override TemplateExpressionParser GetExpressionParser()
        {
            throw new NotSupportedException();
        }
    }

    public abstract class TemplateContext
    {
        internal static NoopTemplateContext NoopTemplateContext = new();

        protected TemplateContext(TemplateDataContainer data, CultureInfo culture)
        {
            Data = data;
            Culture = culture;
        }

        public CultureInfo Culture { get; }

        public Action<TemplateExpressionContext>? TemplateExpressionContextConfiguring { get; set; }

        public TemplateDataContainer Data { get; }

        private List<ITemplateStringFormatter> StringFormatters { get; } = [];

        internal ITemplateStringFormatter? FindStringFormatter(string format)
            => StringFormatters.FirstOrDefault(x => x.CanFormat(format));

        public void AddStringFormatter(Func<string?, bool> canFormat, Func<string?, string, IFormatProvider?, string?> format)
        {
            StringFormatters.Add(new InternalTemplateStringFormatter(canFormat, format));
        }

        public void AddStringFormatter(ITemplateStringFormatter formatter)
        {
            Guard.ArgNotNull(formatter);

            StringFormatters.Add(formatter);
        }

        /// <summary>
        /// Additional properties (and values) provided by external means. E.g. by the FlexEmailDocumentTemplate itself.
        /// </summary>
        public TemplateExternalPropertiesContainer ExternalPropertiesContainer { get; set; } = new();

        /// <summary>
        /// Processes properties defined by the template itself.
        /// </summary>
        public virtual void ProcessCustomProperty(TemplateExpressionContext context)
        {
            if (context.CurrentInstruction == null)
                throw new TemplateException("Current instraction is not assigned.");

            var propName = context.CurrentInstruction.Name;

            var prop = ExternalPropertiesContainer.Items.FirstOrDefault(x => x.Name == propName);
            if (prop == null)
                throw new TemplateException($"External property '{propName}' not found.");

            context.Processor.SetText(prop.Value);

            context.Processor.IsMatch = true;
        }

        readonly ConcurrentDictionary<Guid, TemplateInstructionDefinition> _instructionDefinitionsCache = new();

        /// <summary>
        /// Resolves data root properties and external properties.
        /// </summary>
        public TemplateInstructionDefinition? ResolveInstruction(Type sourceType, string propName)
        {
            if (sourceType == typeof(TemplateExternalPropertiesContainer))
            {
                var prop = ExternalPropertiesContainer.Items.FirstOrDefault(x => x.Name == propName);
                if (prop == null)
                    return null;

                if (_instructionDefinitionsCache.TryGetValue(prop.Guid, out TemplateInstructionDefinition? definition))
                    return definition;

                definition = new TemplateInstructionDefinition<TemplateExternalPropertiesContainer>(typeof(string))
                {
                    Name = prop.Name,
                    IsReturnTypeSimple = true,
                    ExecuteCore = (c, x) => ProcessCustomProperty(c)
                };

                _instructionDefinitionsCache.TryAdd(prop.Guid, definition);

                return definition;
            }
            else if (sourceType == typeof(TemplateDataContainer))
            {
                var prop = Data.FindPropAccessor(propName, null);
                if (prop == null)
                    return null;

                if (_instructionDefinitionsCache.TryGetValue(prop.Guid, out TemplateInstructionDefinition? def))
                    return def;

                var definition = new TemplateInstructionDefinition<TemplateDataContainer>(prop.Type)
                {
                    Name = prop.Name,
                    IsReturnTypeList = prop.Type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.Type),
                    IsReturnTypeSimple = TypeHelper.IsSimple(prop.Type)
                };

                if (definition.IsReturnTypeList)
                {
                    definition.ListValueGetter = (ctx, x) =>
                    {
                        if (ctx.CurrentInstruction == null)
                            throw new TemplateException("Current instraction is not assigned.");

                        var value = x.GetPropValueObject(ctx.CurrentInstruction.Name);
                        if (value is not IEnumerable enumerable)
                            throw new TemplateException("The value was expected to be a list type.");

                        return enumerable.Cast<object>();
                    };
                }
                else
                {
                    definition.ValueGetter = (ctx, data) =>
                    {
                        if (ctx.CurrentInstruction == null)
                            throw new TemplateException("Current instraction is not assigned.");

                        return data.GetPropValueObject(ctx.CurrentInstruction.Name);
                    };
                }

                _instructionDefinitionsCache.TryAdd(prop.Guid, definition);

                return definition;
            }

            return null;
        }

        public abstract TemplateExpressionParser GetExpressionParser();

        public virtual TemplateExpressionContext CreateExpressionContext(
            TemplateProcessor templateProcessor, AstNode ast)
        {
            var context = new TemplateExpressionContext(this, Data, templateProcessor, ast);

            Data.GetRequiredProp<TemplateEnvContainer>().Context = context;

            return context;
        }
    }
}
