using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Templates
{
    public class TemplatePropPathProcessor
    {
        public List<TemplateStepCustomPropBase> CustomPropDefinitions { get; set; } = new List<TemplateStepCustomPropBase>();

        public void AddCustomProperties(IEnumerable<TemplateStepCustomPropBase> definitions)
        {
            CustomPropDefinitions.AddRange(definitions);
        }

        TemplatePropPathParser _pathParser;

        public TemplatePropPathParser GetParser()
        {
            if (_pathParser == null)
            {
                _pathParser = new TemplatePropPathParser();
                _pathParser.CustomPropDefinitions = CustomPropDefinitions;
            }

            return _pathParser;
        }

        public void ExecuteNew(TemplateElemTransformationContext context, object item)
        {
            if (context.Ast == null)
                return;

            //Processor.IsMatch = true;
            ExecuteNewCore(context, item, context.Ast);

            if (context.Values != null && context.Values.Count != 0)
            {
                var value = context.Values.Select(x => x.ToString().Trim()).Join(" ");
            }
        }

        void ExecuteNewCore(TemplateElemTransformationContext context, object contextItem, AstItem node)
        {
            if (node == null)
                return;

            if (node is PropAstNode prop)
            {
                if (contextItem == null)
                    throw new TemplateProcessorException("No context item.");

                if (!prop.DeclaringType.IsAssignableFrom(contextItem.GetType()))
                    throw new TemplateProcessorException($"Invalid template property path state: " +
                        $"current item (type '{contextItem.GetType()}') is not of expected type '{prop.DeclaringType}'.");

                if (prop.ReturnType.IsSimple)
                {
                    if (prop.CustomDefinition != null)
                    {
                        if (prop.CustomDefinition.ExecuteCore != null)
                        {
                            prop.CustomDefinition.ExecuteCore(context, contextItem);
                        }
                        else
                        {
                            var value = prop.CustomDefinition
                                .GetTargetValuesCore(context, contextItem)
                                .Where(x => x != null)
                                .FirstOrDefault();

                            if (value != null)
                                context.Values.Add(value);
                        }
                    }
                    else
                    {
                        var value = prop.PropInfo.GetValue(contextItem);
                        if (value != null)
                            context.Values.Add(value);
                    }

                    return;
                }

                if (prop.CustomDefinition != null)
                {
                    var items = prop.CustomDefinition.GetTargetValuesCore(context, contextItem);
                    foreach (var item in items)
                        ExecuteNewCore(context, item, prop.Right);
                }
                else
                {
                    // Contract.ResponsiblePeople.Where(x.Role.Name == "Hero").Select(x.AnyEmail).Join("; ")
                    var item = prop.PropInfo.GetValue(contextItem);
                    ExecuteNewCore(context, item, prop.Right);
                }
            }
            else if (node is FuncAstNode func)
            {
                func.Execute(context);
            }
        }

        public IEnumerable<object> GetItems(TemplateElemTransformationContext context, object item, AstItem node)
        {
            return GetItemsCore(context, item, node);
        }

        public IEnumerable<object> GetItemsCore(TemplateElemTransformationContext context, object contextItem, AstItem node)
        {
            if (contextItem == null || node == null)
                yield break;

            var prop = node as PropAstNode;
            if (prop == null)
                throw new TemplateProcessorException("A property AST node was expected.");

            if (!prop.DeclaringType.IsAssignableFrom(contextItem.GetType()))
                throw new TemplateProcessorException($"Invalid template property path state: " +
                    $"current item (type '{contextItem.GetType()}') is not of expected type '{prop.DeclaringType}'.");

            // If primitive values ahead then return the current context item and stop.
            if (prop.ReturnType.IsSimple)
            {
                yield return contextItem;
                yield break;
            }

            if (prop.CustomDefinition != null)
            {
                var values = prop.CustomDefinition.GetTargetValuesCore(context, contextItem);
                foreach (var item in values)
                    foreach (var res in GetItemsCore(context, item, prop.Right))
                        yield return res;
            }
            else
            {
                foreach (var res in GetItemsCore(context, prop.PropInfo.GetValue(contextItem), prop.Right))
                    yield return res;
            }
        }
    }
}
