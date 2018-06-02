using Nito.AsyncEx;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Templates
{
    public class TemplateExpressionProcessor
    {
        public void Execute(TemplateExpressionContext context)
        {
            Guard.ArgNotNull(context, nameof(context));

            if (context.Ast == null)
                return;

            if (context.Ast is CSharpScriptAstNode scriptNode)
            {
                var value = AsyncContext.Run(() => scriptNode.Script.RunAsync(context.DataContainer));

                context.SetReturnValue(ToEnumerable(value));

                //if (context.Value is string text)
                //    System.Diagnostics.Debug.WriteLine("Text: " + text);
                //else if (context.Value is IEnumerable enumerable)
                //    System.Diagnostics.Debug.WriteLine("List: " + enumerable?.Cast<object>().Select(x => x.ToString()).Join(", "));
                //else
                //    System.Diagnostics.Debug.WriteLine("Single: " + context.Value?.ToString());
            }
            else
            {
                var values = ExecuteCore(context, context.DataContainer, context.Ast);
                if (values.Count() != 0)
                    context.SetReturnValue(values);
            }
        }

        IEnumerable<object> ExecuteCore(TemplateExpressionContext context, object contextObj, AstNode node)
        {
            Guard.ArgNotNull(contextObj, nameof(contextObj));
            Guard.ArgNotNull(node, nameof(node));

            var values = Enumerable.Empty<object>();

            if (node is InstructionAstNode instruction)
            {
                context.CurrentInstruction = instruction;

                if (!instruction.SourceType.IsAssignableFrom(contextObj.GetType()))
                    throw new TemplateProcessorException($"Invalid template expression state: " +
                        $"context object (type '{contextObj.GetType()}') is not of expected type '{instruction.SourceType}'.");

                if (instruction.Definition != null)
                {
                    // Handle custom instructions.

                    if (instruction.Definition.ExecuteCore != null)
                    {
                        // This is an executing instruction.
                        // Such instructions have to value getter and will
                        // modify the output directly.

                        if (context.IsModificationDenied)
                            throw new TemplateProcessorException("Invalid template expression. " +
                                "The current mode does not allow modification of the transformation's output.");

                        if (instruction.Right != null)
                            throw new TemplateProcessorException("Invalid template expression. " +
                                "Custom executing instructions must appear at last position in the expression.");

                        instruction.Definition.ExecuteCore(context, contextObj);

                        // Executing instructions do not have return values.
                        return Enumerable.Empty<object>();
                    }
                    else
                    {
                        // Call instruction's values getter.
                        values = instruction.Definition.GetValuesCore(context, contextObj)
                            ?? Enumerable.Empty<object>();

                        //if (values != null)
                        //    context.SetValue(values);
                    }
                    context.CurrentInstruction = null;
                }
                else
                {
                    // Get property value.
                    var propValue = instruction.PropInfo.GetValue(contextObj);
                    values = ToEnumerable(propValue);
                }

                if (instruction.Right != null)
                {
                    // Execute next instruction.

                    if (values.Count() > 1)
                        throw new TemplateProcessorException("Invalid template expression. " +
                            "Intermediate instructions of normal expressions must not return more than one value. " +
                            "Use CSharp expressions instead.");

                    var value = values.FirstOrDefault();

                    if (value != null)
                    {
                        // Execute next instructions and set return values.
                        values = ExecuteCore(context, value, instruction.Right);
                    }
                    else
                        values = new[] { (object)null };
                }

                return values;
            }
            else
            {
                throw new TemplateProcessorException("Invalid template expression. " +
                    $"Unexpected type of syntax node '{node.GetType()}'.");
            }
        }

        IEnumerable<object> ToEnumerable(object value)
        {
            if (value is IEnumerable enumerable && !(value is string))
                return ((IEnumerable)enumerable).Cast<object>();
            else
                return new[] { value };
        }

        public IEnumerable<object> GetItems(TemplateExpressionContext context, object item, AstNode node)
        {
            return GetItemsCore(context, item, node);
        }

        public IEnumerable<object> GetItemsCore(TemplateExpressionContext context, object contextItem, AstNode node)
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
