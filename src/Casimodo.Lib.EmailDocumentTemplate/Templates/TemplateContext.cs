using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#nullable enable

namespace Casimodo.Lib.Templates
{
    public abstract class TemplateContext
    {
        protected TemplateContext(CultureInfo culture)
        {
            Culture = culture;
        }

        public CultureInfo Culture { get; }

        public Action<TemplateExpressionContext> TemplateExpressionContextConfiguring { get; set; }

        public TemplateDataContainer Data { get; set; }

        private List<ITemplateStringFormatter> StringFormatters { get; } = new();

        internal ITemplateStringFormatter? FindStringFormatter(string format)
          => StringFormatters.FirstOrDefault(x => x.CanFormat(format));

        public void AddStringFormatter(Func<string?, bool> canFormat, Func<string?, string, IFormatProvider?, string?> format)
        {
            StringFormatters.Add(new InternalTemplateStringFormatter(canFormat, format));
        }

        public void AddStringFormatter(ITemplateStringFormatter formatter)
        {
            Guard.ArgNotNull(formatter, nameof(formatter));

            StringFormatters.Add(formatter);
        }

        /// <summary>
        /// Additional properties (and values) provided by external means. E.g. by the FlexEmailDocumentTemplate itself.
        /// </summary>
        public TemplateExternalPropertiesContainer ExternalPropertiesContainer { get; set; }
            = new TemplateExternalPropertiesContainer();

        /// <summary>
        /// Processes properties defined by the template itself.
        /// </summary>
        public virtual void ProcessCustomProperty(TemplateExpressionContext context)
        {
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

                definition = new TemplateInstructionDefinition<TemplateExternalPropertiesContainer>
                {
                    Name = prop.Name,
                    ReturnType = typeof(string),
                    IsReturnTypeSimple = true
                };

                definition.ExecuteCore = (c, x) => ProcessCustomProperty(c);

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

                var definition = new TemplateInstructionDefinition<TemplateDataContainer>
                {
                    Name = prop.Name,
                    ReturnType = prop.Type,
                    IsReturnTypeList = prop.Type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.Type),
                    IsReturnTypeSimple = TypeHelper.IsSimple(prop.Type)
                };

                if (definition.IsReturnTypeList)
                {
                    definition.ListValueGetter = (c, x) =>
                    {
                        var value = x.GetPropValueObject(c.CurrentInstruction.Name);
                        if (value is not IEnumerable enumerable)
                            throw new TemplateException("The value was expected to be a list type.");

                        return enumerable.Cast<object>();
                    };
                }
                else
                {
                    definition.ValueGetter = (c, x) => x.GetPropValueObject(c.CurrentInstruction.Name);
                }

                _instructionDefinitionsCache.TryAdd(prop.Guid, definition);

                return definition;
            }

            return null;
        }

        public abstract TemplateExpressionParser GetExpressionParser();

        public virtual TemplateExpressionContext CreateExpressionContext(TemplateProcessor? templateProcessor)
        {
            var context = new TemplateExpressionContext();
            context.CoreContext = this;
            context.DataContainer = Data;
            context.Processor = templateProcessor;

            Data.GetRequiredProp<TemplateEnvContainer>().Context = context;

            return context;
        }
    }
}
