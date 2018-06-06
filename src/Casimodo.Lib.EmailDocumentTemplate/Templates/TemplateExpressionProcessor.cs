﻿using Nito.AsyncEx;
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
                    throw new TemplateException($"Invalid template expression state: " +
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
                            throw new TemplateException("Invalid template expression. " +
                                "The current mode does not allow modification of the transformation's output.");

                        if (instruction.Right != null)
                            throw new TemplateException("Invalid template expression. " +
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
                        throw new TemplateException("Invalid template expression. " +
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
                throw new TemplateException("Invalid template expression. " +
                    $"Unexpected type of syntax node '{node.GetType().Name}'.");
            }
        }

        IEnumerable<object> ToEnumerable(object value)
        {
            if (value is IEnumerable enumerable && !(value is string))
                return enumerable.Cast<object>();
            else
                return new[] { value };
        }
    }
}
