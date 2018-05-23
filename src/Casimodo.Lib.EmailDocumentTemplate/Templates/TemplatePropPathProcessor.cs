using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Templates
{
    public class TemplatePropPathProcessor
    {
        public List<TemplateStepCustomPropBase> CustomPropDefinitions { get; set; }

        TemplatePropPathParser _pathParser;

        TemplatePropPathParser GetPathParser()
        {
            if (_pathParser == null)
            {
                _pathParser = new TemplatePropPathParser();
                _pathParser.CustomPropDefinitions = CustomPropDefinitions;
            }

            return _pathParser;
        }

        public void ExecuteNew(TemplateTransformation trans, object item, Type itemType, string prop, string propPath)
        {
            var context = new TemplateElemTransformationContext();

            context.Ast = GetPathParser().ParsePropPath(itemType, propPath);
            if (context.Ast != null)
            {
                //Processor.IsMatch = true;
                ExecuteNewCore(context, item, context.Ast);

                if (context.Values != null && context.Values.Count != 0)
                {
                    var value = context.Values.Select(x => x.ToString().Trim()).Join(" ");
                }
            }
        }

        void ExecuteNewCore(TemplateElemTransformationContext context, object contextItem, SelectorAstItem node)
        {
            if (node == null)
                return;

            if (node is PropAstItem propExpr)
            {
                if (contextItem == null)
                    throw new TemplateProcessorException("No context item.");

                if (!propExpr.SourceType.IsAssignableFrom(contextItem.GetType()))
                    throw new TemplateProcessorException($"Invalid template property path state: " +
                        $"current item (type '{contextItem.GetType()}') is not of expected type '{propExpr.SourceType}'.");

                if (propExpr.IsLeafValue)
                {
                    if (propExpr.CustomDefinition != null)
                    {
                        if (propExpr.CustomDefinition.ExecuteCore != null)
                        {
                            propExpr.CustomDefinition.ExecuteCore(context.Transformation, contextItem);
                        }
                        else
                        {
                            var value = propExpr.CustomDefinition
                                .GetTargetValuesCore(contextItem)
                                .Where(x => x != null)
                                .FirstOrDefault();

                            if (value != null)
                                context.Values.Add(value);
                        }
                    }
                    else
                    {
                        var value = propExpr.SourcePropInfo.GetValue(contextItem);
                        if (value != null)
                            context.Values.Add(value);
                    }

                    return;
                }

                if (propExpr.CustomDefinition != null)
                {
                    var items = propExpr.CustomDefinition.GetTargetValuesCore(contextItem);
                    foreach (var item in items)
                        ExecuteNewCore(context, item, propExpr.Right);
                }
                else
                {
                    // Contract.ResponsiblePeople.Where(x.Role.Name == "Hero").Select(x.AnyEmail).Join("; ")
                    var item = propExpr.SourcePropInfo.GetValue(contextItem);
                    ExecuteNewCore(context, item, propExpr.Right);
                }
            }
            else if (node is FuncAstItem funcExpr)
            {
                funcExpr.Execute(context);
            }
        }

        public IEnumerable<object> GetItems(object item, SelectorAstItem node)
        {
            return GetItemsCore(item, node);
        }

        public IEnumerable<object> GetItemsCore(object contextItem, SelectorAstItem node)
        {
            if (contextItem == null || node == null)
                yield break;

            var prop = node as PropAstItem;
            if (prop == null)
                throw new TemplateProcessorException("A property AST node was expected.");

            if (!prop.SourceType.IsAssignableFrom(contextItem.GetType()))
                throw new TemplateProcessorException($"Invalid template property path state: " +
                    $"current item (type '{contextItem.GetType()}') is not of expected type '{prop.SourceType}'.");

            // If primitive values ahead then return the current context item and stop.
            if (prop.IsLeafValue)
            {
                yield return contextItem;
                yield break;
            }

            if (prop.CustomDefinition != null)
            {
                var values = prop.CustomDefinition.GetTargetValuesCore(contextItem);
                foreach (var item in values)
                    foreach (var res in GetItemsCore(item, prop.Right))
                        yield return res;
            }
            else
            {
                foreach (var res in GetItemsCore(prop.SourcePropInfo.GetValue(contextItem), prop.Right))
                    yield return res;
            }
        }
    }
}
