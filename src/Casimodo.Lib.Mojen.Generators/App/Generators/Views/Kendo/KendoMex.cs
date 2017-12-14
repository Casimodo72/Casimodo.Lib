using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public abstract class MexGeneratorBase<TThis> : MojenGeneratorBase
        where TThis : MexGeneratorBase<TThis>
    {
        StringBuilder _sb = new StringBuilder();

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
            if (item is MexExpressionNode)
            {
                var node = (MexExpressionNode)item;

                if (node.Left != null)
                    O(node.Left);

                if (node.Op != MexOp.None)
                    O(node.Op);

                if (node.Right != null)
                    O(node.Right);
            }
            else if (item is MexCondition)
            {
                Build((MexCondition)item);
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

    public class KendoDataSourceMex : MexGeneratorBase<KendoDataSourceMex>
    {
        public static string ToKendoDataSourceFilters(MexExpressionNode node)
        {
            return new KendoDataSourceMex().BuildKendoDataSourceFilter(node).ToString();
        }

        public KendoDataSourceMex BuildKendoDataSourceFilter(MexExpressionNode expression)
        {
            o("[");
            Build(expression);
            o("]");
            return this;
        }

        protected override string ToValue(MexItem item)
        {
            return MojenUtils.ToJsValue(((MexValue)item).Value, parse: true);
        }

        protected override string ToPropPath(MexItem item)
        {
            return item.AsProp().PropPath;
        }

        public override KendoDataSourceMex Build(MexCondition condition)
        {
            o("{");

            o(" field: '"); O(condition.Left); o("'");

            o(", operator: '"); O(condition.Op); o("'");

            o(", value: "); O(condition.Right);

            o(" }");

            return This();
        }

        protected override void O(MexOp op)
        {
            if (op == MexOp.And)
                // This is just a list separator of conditions.
                o(", ");
            else
                o(_odataOps[op]);
        }

        Dictionary<MexOp, string> _odataOps = new Dictionary<MexOp, string>
        {
            { MexOp.Eq, "eq"},
            { MexOp.Neq, "ne"},
            { MexOp.Gr, "gt"},
            { MexOp.GrOrEq, "ge"},
            { MexOp.Less, "lt"},
            { MexOp.LessOrEq, "le"},
            { MexOp.And, "and"},
            { MexOp.Or, "or"},
        };

        /*
        OData operators
        ~~~~~~~~~~~~~~~
        Operator 	Description 	Example
        Logical Operators
        Eq 	Equal 	/Suppliers?$filter=Address/City eq 'Redmond'
        Ne 	Not equal 	/Suppliers?$filter=Address/City ne 'London'
        Gt 	Greater than 	/Products?$filter=Price gt 20
        Ge 	Greater than or equal 	/Products?$filter=Price ge 10
        Lt 	Less than 	/Products?$filter=Price lt 20
        Le 	Less than or equal 	/Products?$filter=Price le 100
        And 	Logical and 	/Products?$filter=Price le 200 and Price gt 3.5
        Or 	Logical or 	/Products?$filter=Price le 3.5 or Price gt 200
        Not 	Logical negation 	/Products?$filter=not endswith(Description,'milk')
        Arithmetic Operators
        Add 	Addition 	/Products?$filter=Price add 5 gt 10
        Sub 	Subtraction 	/Products?$filter=Price sub 5 gt 10
        Mul 	Multiplication 	/Products?$filter=Price mul 2 gt 2000
        Div 	Division 	/Products?$filter=Price div 2 gt 4
        Mod 	Modulo 	/Products?$filter=Price mod 2 eq 0
        Grouping Operators
        ( ) 	Precedence grouping 	/Products?$filter=(Price sub 5) gt 10
        */


    }
}
