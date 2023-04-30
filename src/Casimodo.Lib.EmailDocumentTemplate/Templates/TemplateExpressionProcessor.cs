using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Casimodo.Lib.Templates
{
    public class TemplateExpressionProcessor
    {
        public async Task<IEnumerable<object>> FindObjects(TemplateCoreContext coreContext, TemplateExpression expression)
        {
            var context = await ParseAndEvaluateValue(coreContext, expression);

            return context.HasReturnValue ? context.ReturnValue : Enumerable.Empty<object>();
        }

        public async Task<bool> EvaluateCondition(TemplateCoreContext coreContext, TemplateExpression expression)
        {
            var context = await ParseAndEvaluateValue(coreContext, expression);

            return context.HasReturnValue ? ConditionResultToBoolean(context.ReturnValue) : false;
        }

        public async Task<object> EvaluateValue(TemplateCoreContext coreContext, TemplateExpression expression)
        {
            var context = await ParseAndEvaluateValue(coreContext, expression);

            return context.HasReturnValue ? EvaluatedResultToValue(context.ReturnValue) : null;
        }

        public async Task<TemplateExpressionContext> ParseAndEvaluateValue(TemplateCoreContext coreContext, TemplateExpression expression)
        {
            Guard.ArgNotNull(expression, nameof(expression));

            var context = coreContext.CreateExpressionContext(templateProcessor: null);
            // We do not want to modify the template here. We just want to get a value. 
            context.IsModificationDenied = true;
            context.Ast = ParseExpression(coreContext, expression);

            await ExecuteAsync(context);

            return context;
        }

        public static AstNode ParseExpression(TemplateCoreContext coreContext, TemplateExpression element)
        {
            return coreContext.GetExpressionParser().ParseTemplateExpression(coreContext.Data, element.Expression, element.Kind);
        }

        public static object EvaluatedResultToValue(IEnumerable<object> evaluatedResult)
        {
            if (evaluatedResult == null)
                return null;

            var value = evaluatedResult.FirstOrDefault();
            if (value == null)
                return null;

            return value;
        }

        public static bool ConditionResultToBoolean(IEnumerable<object> evaluatedResult)
        {
            if (evaluatedResult == null)
                return false;

            var value = evaluatedResult.FirstOrDefault();
            if (value == null)
                return false;

            if (value is bool bval)
                return bval;
            else if (value is string sval)
                return !string.IsNullOrEmpty(sval);
            else if (value is int ival)
                return ival > 0;
            else if (value is decimal dval)
                return dval > 0;

            return true;
        }

        public async Task ExecuteAsync(TemplateExpressionContext context)
        {
            Guard.ArgNotNull(context, nameof(context));

            if (context.Ast == null)
                return;

            if (context.Ast is CSharpScriptAstNode scriptNode)
            {
                var value = await scriptNode.Script.RunAsync(context.DataContainer);

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

                    if (instruction.ReturnType.IsListType)
                    {                        
                        values = ExecuteCore(context, values, instruction.Right);
                    }
                    else
                    {
                        // TODO: REMOVE
                        //if (values.Count() > 1)
                        //    throw new TemplateException("Invalid template expression. " +
                        //        "Intermediate instructions of normal expressions must not return more than one value. " +
                        //        "Use CSharp expressions instead.");

                        var value = values.FirstOrDefault();

                        if (value != null)
                        {
                            // Execute next instructions and set return values.
                            values = ExecuteCore(context, value, instruction.Right);
                        }
                        else
                            values = new[] { (object)null };
                    }
                }

                return values;
            }
            else
            {
                throw new TemplateException("Invalid template expression. " +
                    $"Unexpected type of syntax node '{node.GetType().Name}'.");
            }
        }

        static IEnumerable<object> ToEnumerable(object value)
        {
            if (value is IEnumerable enumerable && value is not string)
                return enumerable.Cast<object>();
            else
                return new[] { value };
        }
    }
}
