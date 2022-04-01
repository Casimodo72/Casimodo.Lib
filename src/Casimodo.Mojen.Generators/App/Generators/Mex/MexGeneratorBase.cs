using System.IO;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public abstract class MexGeneratorBase<TThis> : MojenGeneratorBase
        where TThis : MexGeneratorBase<TThis>
    {
        readonly StringBuilder _sb = new();

        public MexGeneratorBase()
        {
            Use(new StringWriter(_sb));
        }

        protected void CheckCondition(MexExpressionNode condition)
        {
            if (condition.Right != null && condition.Op == MexOp.None)
                throw new MexException("Right AST node must not be assigned if operation is none.");

            if (condition.Right == null && condition.Op != MexOp.None)
                throw new MexException("Right AST is not assigned.");
        }

        protected abstract string ToValue(MexItem item);
        protected abstract string ToPropPath(MexItem item);
        protected abstract void O(MexOp op);

        public virtual TThis Build(MexCondition condition)
        {
            O(condition.Left);
            O(condition.Op);
            O(condition.Right);

            return This();
        }

        public TThis Build(MexExpressionNode condition)
        {
            CheckCondition(condition);

            O(condition.Left);

            if (condition.Op != MexOp.None)
            {
                O(condition.Op);
                O(condition.Right);
            }

            return This();
        }

        protected void O(MexValue value)
        {
            if (value.Value == null)
                o("null");
            else
                o(value.Value.ToString());
        }

        protected void O(MexItem item)
        {
            if (item is MexExpressionNode node)
            {
                if (node.Left != null)
                    O(node.Left);

                if (node.Op != MexOp.None)
                    O(node.Op);

                if (node.Right != null)
                    O(node.Right);
            }
            else if (item is MexCondition condition)
            {
                Build(condition);
            }
            else if (item is MexProp)
            {
                o(ToPropPath(item));
            }
            else if (item is MexValue)
            {
                o(ToValue(item));
            }
            else throw new MojenException($"Unexpected Mex item '{item.GetType().Name}'.");
        }

        public TThis This()
        {
            return (TThis)this;
        }

        public override string ToString()
        {
            Writer.Flush();
            return _sb.ToString();
        }
    }
}
