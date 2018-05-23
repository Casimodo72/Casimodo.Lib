using Casimodo.Lib.SimpleParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Templates
{
    public class TemplatePropPathParser
    {
        static readonly List<PropStepFuncDef> _functionsDefs = new List<PropStepFuncDef>
        {
            new PropStepFuncDef { Name = "Where", IsListFunc = true },
            new PropStepFuncDef { Name = "Join", IsListFunc = true }
        };

        Type CurItemType { get; set; }

        public List<SelectorAstItem> Steps { get; set; } = new List<SelectorAstItem>();
        public List<TemplateStepCustomPropBase> CustomPropDefinitions { get; set; }

        SimpleStringParser _p { get; set; }

        public SelectorAstItem ParsePropPath(Type contextItemType, string path)
        {
            Guard.ArgNotNull(contextItemType, nameof(contextItemType));
            Guard.ArgNotEmpty(path, nameof(path));

            _p = new SimpleStringParser(path);

            Clear();
            try
            {
                return ParseAny(contextItemType, null);
            }
            finally
            {
                Clear();
            }
        }
        SelectorAstItem ParseAny(Type contextItemType, PropAstItem parent)
        {
            Type prevItemType = CurItemType;
            try
            {
                CurItemType = contextItemType;
                // Contract.ResponsiblePeople.Select(Addresses.Select(AnyEmail)).Join("; ")

                _p.SkipWsp();

                while (!_p.IsEnd && (!_p.Is('.', '(')))
                    _p.Consume();

                var token = _p.GetConsumedText().Trim();

                // Parse function
                if (_p.Is("("))
                {
                    return ParseFunction(token, parent);
                }

                _p.Skip(".");

                // Handle custom properties (or overrides of normal properties) first.
                var prop = ParseCustomProp(token);
                if (prop == null)
                {
                    // Handle property.
                    var iprop = CurItemType.GetProperty(token);
                    if (iprop != null)
                    {
                        prop = CreatePropNode();
                        prop.SourcePropInfo = iprop;
                        prop.TargetType = iprop.PropertyType;
                        prop.IsLeafValue = IsSimple(iprop.PropertyType);
                        //CurStep.TargetValue = prop.GetValue(CurItem);
                    }
                }

                if (prop != null)
                {
                    CurItemType = prop.TargetType;
                    var sub = ParseAny(prop.TargetType, prop);
                    if (sub != null)
                        prop.Right = sub;

                    return prop;
                }

                throw new TemplateProcessorException($"Invalid expression '{token}'.");
            }
            finally
            {
                CurItemType = prevItemType;
            }
        }

        void Clear()
        {
            //CurItemType = null;
            Steps.Clear();
        }

        PropAstItem CreatePropNode()
        {
            var node = new PropAstItem
            {
                //SourceType = CurItemType
            };

            //Steps.Add(CurStep);

            return node;
        }

        PropAstItem ParseCustomProp(string expression)
        {
            if (CustomPropDefinitions == null)
                return null;

            var custom = CustomPropDefinitions.FirstOrDefault(x =>
                x.PropName == expression &&
                x.SourceType.IsAssignableFrom(CurItemType));

            if (custom == null)
                return null;

            var node = CreatePropNode();
            node.CustomDefinition = custom;
            node.SourceProp = custom.PropName;
            node.IsList = custom.IsList;
            node.TargetType = custom.TargetType;
            node.IsLeafValue = custom.IsLeafValue;

            return node;
        }

        SelectorAstItem ParseFunction(string funcName, PropAstItem contextPropNode)
        {
            if (!_p.Skip("(")) throw new TemplateProcessorException($"A function was expected.");
            _p.SkipWsp();
            _p.ConsumeTo(")");
            var callBody = _p.GetConsumedText();
            _p.Skip(")");
            _p.SkipWsp();
            _p.Skip(".");

            var funcDef = _functionsDefs.FirstOrDefault(x => x.Name == funcName);
            if (funcDef == null)
                throw new TemplateProcessorException($"Function '{funcName}' not found.");

            if (funcDef.IsListFunc && !contextPropNode.IsList)
                throw new TemplateProcessorException($"Function '{funcName}': source is not a list.");

            if (!funcDef.IsListFunc && contextPropNode.IsList)
                throw new TemplateProcessorException($"Function '{funcName}': source is a list.");

            var func = new FuncAstItem
            {
            };

            func.Definition = funcDef;

            return func;
        }

        internal static bool IsSimple(Type type)
        {
            // Source: https://stackoverflow.com/questions/863881/how-do-i-tell-if-a-type-is-a-simple-type-i-e-holds-a-single-value

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // Nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }

            return type.IsPrimitive || type.IsEnum || type.Equals(typeof(string)) || type.Equals(typeof(decimal));
        }
    }
}
