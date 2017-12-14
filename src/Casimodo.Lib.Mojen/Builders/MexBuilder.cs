using System;

namespace Casimodo.Lib.Mojen
{
    public class MexBuilder
    { }

    public class MexSelectionBuilder : MexBuilder
    {
        public MexPropSelection Selector { get; set; } = new MexPropSelection();

        public MexSelectionBuilder From(string type)
        {
            Selector.TypeName = type;
            return this;
        }

        public MexSelectionBuilder From(MojType type)
        {
            Guard.ArgNotNull(type, nameof(type));

            Selector.TypeName = type.Name;
            return this;
        }

        public MexSelectionBuilder Select(string prop)
        {
            Selector.PropName = prop;
            return this;
        }
    }

    public class MexConditionBuilder : MexBuilder
    {
        public MexConditionBuilder()
        {
            Cur = Expression;
        }

        public MexExpressionNode Expression { get; set; } = new MexExpressionNode();

        public MexExpressionNode Cur { get; set; }

        public MexConditionBuilder Where(string path, object value)
        {
            return Where(path, MexOp.Eq, value);
        }

        public MexConditionBuilder Where(string path, MexOp op, object value)
        {
            return Where(Prop(path), op, value is MexItem ? (MexItem)value : Value(value));
        }        

        public MexConditionBuilder Where(MexItem left, MexOp op, MexItem right)
        {
            var condition = new MexCondition
            {
                Left = left,
                Op = op,
                Right = right
            };

            if (Cur.Left == null)
                Cur.Left = condition;
            else
            {
                Cur.Op = MexOp.And;
                Cur.Right = new MexExpressionNode
                {
                    Op = MexOp.None,
                    Left = condition
                };

                Cur = (MexExpressionNode)Cur.Right;
            }

            return this;
        }

        public MexValue Value(object value)
        {
            return new MexValue
            {
                Value = value
            };
        }

        public MexProp Prop(string path)
        {
            return new MexProp
            {
                PropPath = path
            };
        }

        public static MexExpressionNode BuildCondition(Action<MexConditionBuilder> build)
        {
            Guard.ArgNotNull(build, nameof(build));

            var conditionBuilder = new MexConditionBuilder();
            build(conditionBuilder);
            return conditionBuilder.Expression;
        }
    }
}