using Nito.AsyncEx;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Templates
{
    public class TemplateExpressionProcessor
    {
        public void Execute(TemplateElemTransformationContext context)
        {
            Guard.ArgNotNull(context, nameof(context));

            if (context.Ast == null)
                return;

            if (context.Ast is CSharpScriptAstNode scriptNode)
            {
                context.SetValue(AsyncContext.Run(() => scriptNode.Script.RunAsync(context.DataContainer)));

                //if (context.Value) is string text)
                //    System.Diagnostics.Debug.WriteLine("Text: " + text);
                //else if (context.Value) is IEnumerable enumerable)
                //    System.Diagnostics.Debug.WriteLine("List: " + enumerable?.Cast<object>().Select(x => x.ToString()).Join(", "));
                //else
                //    System.Diagnostics.Debug.WriteLine("Single: " + context.Value?.ToString());
            }
            else
            {
                ExecuteCore(context, context.DataContainer, context.Ast);
            }

            // NOTE: Set value only if it was provided, because instructions might
            //   not return any value but manipulate the output directly instead.
            if (context.HasValue)
                context.Transformation.SetText(context.Value);
        }

        void ExecuteCSharpExpression(TemplateElemTransformationContext context)
        {
            var scriptNode = context.Ast as CSharpScriptAstNode;
            if (scriptNode == null)
                return;

            var resultObj = AsyncContext.Run(() => scriptNode.Script.RunAsync(context.DataContainer));

            //if (resultObj is string text)
            //    System.Diagnostics.Debug.WriteLine("Text: " + (string)resultObj);
            //else if (resultObj is IEnumerable enu)
            //    System.Diagnostics.Debug.WriteLine("List: " + enu?.Cast<object>().Select(x => x.ToString()).Join(", "));
            //else
            //    System.Diagnostics.Debug.WriteLine("Single: " + resultObj?.ToString());

            context.Transformation.SetText(resultObj);
        }

        void ExecuteCore(TemplateElemTransformationContext context, object contextObj, AstNode node)
        {
            if (contextObj == null || node == null)
                return;

            if (node is InstructionAstNode prop)
            {
                context.CurrentInstruction = prop;

                if (!prop.SourceType.IsAssignableFrom(contextObj.GetType()))
                    throw new TemplateProcessorException($"Invalid template expression state: " +
                        $"context object (type '{contextObj.GetType()}') is not of expected type '{prop.SourceType}'.");

                if (prop.ReturnType.IsSimpleType)
                {
                    if (prop.Definition != null)
                    {

                        if (prop.Definition.ExecuteCore != null)
                        {
                            prop.Definition.ExecuteCore(context, contextObj);
                        }
                        else
                        {
                            var value = prop.Definition
                                .GetValuesCore(context, contextObj)
                                .Where(x => x != null)
                                .FirstOrDefault();

                            if (value != null)
                                context.SetValue(value);
                        }
                        context.CurrentInstruction = null;
                    }
                    else
                    {
                        var value = prop.PropInfo.GetValue(contextObj);
                        if (value != null)
                            context.SetValue(value);
                    }

                    return;
                }

                if (prop.Definition != null)
                {
                    var items = prop.Definition.GetValuesCore(context, contextObj);
                    foreach (var item in items)
                        ExecuteCore(context, item, prop.Right);
                }
                else
                {
                    // Get value via reflection.
                    var item = prop.PropInfo.GetValue(contextObj);
                    ExecuteCore(context, item, prop.Right);
                }
            }
            else
            {
                throw new TemplateProcessorException($"Unexpected type of AST node '{node.GetType()}'.");
            }
        }

        public IEnumerable<object> GetItems(TemplateElemTransformationContext context, object item, AstNode node)
        {
            return GetItemsCore(context, item, node);
        }

        public IEnumerable<object> GetItemsCore(TemplateElemTransformationContext context, object contextItem, AstNode node)
        {
            if (contextItem == null || node == null)
                yield break;

            var prop = node as InstructionAstNode;
            if (prop == null)
                throw new TemplateProcessorException("A property AST node was expected.");

            if (!prop.SourceType.IsAssignableFrom(contextItem.GetType()))
                throw new TemplateProcessorException($"Invalid template property path state: " +
                    $"current item (type '{contextItem.GetType()}') is not of expected type '{prop.SourceType}'.");

            // If primitive values ahead then return the current context item and stop.
            if (prop.ReturnType.IsSimpleType)
            {
                yield return contextItem;
                yield break;
            }

            if (prop.Definition != null)
            {
                var values = prop.Definition.GetValuesCore(context, contextItem);
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
