using System.Collections.Generic;

namespace Casimodo.Lib.Mojen
{
    public class KendoDataSourceMex : MexGeneratorBase<KendoDataSourceMex>
    {
        /// <summary>
        /// Returns comma separated kendo data source filter items.
        /// </summary>
        public static string ToKendoDataSourceFilters(MexExpressionNode node)
        {
            return new KendoDataSourceMex().BuildKendoDataSourceFilters(node).ToString();
        }

        public KendoDataSourceMex BuildKendoDataSourceFilters(MexExpressionNode expression)
        {
            Build(expression);
            return this;
        }

        protected override string ToValue(MexItem item)
        {
            return Moj.JS(((MexValue)item).Value, parse: true);
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
