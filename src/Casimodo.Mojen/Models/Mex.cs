using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Casimodo.Lib.Data;
using System;

namespace Casimodo.Lib.Mojen
{
    public class MexException : Exception
    {
        public MexException(string message)
            : base(message)
        { }
    }

    public class Mex : MojenGeneratorBase
    {
        StringBuilder _sb = new StringBuilder();

        public static string ToLinqPredicate(MexExpressionNode node)
        {
            return new Mex().BuildLinqPredicate(node).ToString();
        }

        public Mex()
        {
            Use(new StringWriter(_sb));
        }

        public Mex BuildLinqWhereClause(IEnumerable<MexExpressionNode> nodes)
        {
            foreach (var node in nodes)
            {
                o(".Where(x => "); Build(node); o(")");
            }
            return this;
        }

        public Mex BuildLinqPredicate(MexExpressionNode node)
        {
            Build(node);

            return this;
        }

        public Mex Build(MexExpressionNode condition)
        {
            if (condition.Right != null && condition.Op == MexOp.None)
                throw new MexException("Right AST node must not be assigned if operation is none.");

            if (condition.Right == null && condition.Op != MexOp.None)
                throw new MexException("Right AST is not assigned.");

            O(condition.Left);

            if (condition.Op != MexOp.None)
            {
                O(condition.Op);
                O(condition.Right);
            }

            return this;
        }

        public Mex BuildLinq(IEnumerable<MexCondition> conditions)
        {
            foreach (var condition in conditions)
                BuildLinq(condition);
            return this;
        }

        public Mex BuildLinq(MexCondition condition)
        {
            o(".Where(x => x."); Build(condition); o(")");

            return this;
        }

        public Mex Build(MexCondition condition)
        {
            O(condition.Left);
            O(condition.Op);
            O(condition.Right);

            return this;
        }

        void O(MexValue value)
        {
            if (value.Value == null)
                o("null");
            else
                o(value.Value.ToString());
        }

        void O(MexItem item)
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
                var prop = (MexProp)item;
                var path = char.IsLower(prop.PropPath[0]) ? prop.PropPath : "x." + prop.PropPath;
                o(path);
            }
            else if (item is MexValue)
            {
                o(Moj.CS(((MexValue)item).Value, parse: true));
            }
            else throw new MojenException($"Unexpected Mex item '{item.GetType().Name}'.");
        }

        void O(MexOp op)
        {
            o(_ops[op]);
        }

        Dictionary<MexOp, string> _ops = new Dictionary<MexOp, string>
        {
            { MexOp.Eq, " == "},
            { MexOp.Neq, " != "},
            { MexOp.Gr, " > "},
            { MexOp.GrOrEq, " >= "},
            { MexOp.Less, " < "},
            { MexOp.LessOrEq, " <= "},
            { MexOp.And, " && "},
            { MexOp.Or, " || "},
        };

        public override string ToString()
        {
            Writer.Flush();
            return _sb.ToString();
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public abstract class MexItem
    { }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexProp : MexItem
    {
        [DataMember]
        public string PropPath { get; set; }

        [DataMember]
        public MojProp Prop { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexType : MexItem
    {
        [DataMember]
        public MojType Type { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexValue : MexItem
    {
        [DataMember]
        public object Value { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public enum MexOp
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        Eq = 1,

        [EnumMember]
        Neq = 1 << 1,

        [EnumMember]
        Gr = 1 << 2,

        [EnumMember]
        Less = 1 << 3,

        [EnumMember]
        GrOrEq = 1 << 4,

        [EnumMember]
        LessOrEq = 1 << 5,

        [EnumMember]
        Contains = 1 << 6,

        [EnumMember]
        StartsWith = 1 << 7,

        [EnumMember]
        EndsWith = 1 << 8,

        /// <summary>
        /// Used for OData $expand query option.
        /// </summary>
        [EnumMember]
        Expand = 1 << 9,

        [EnumMember]
        And = 1 << 10,

        [EnumMember]
        Or = 1 << 11,
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexFunction : MexItem
    {
        public MexFunction()
        {
            Parameters = new MexCollection();
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Kind { get; set; }

        [DataMember]
        public MexCollection Parameters { get; private set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexPropSelection : MexItem
    {
        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string PropName { get; set; }

    }

    public static class MexItemExtensions
    {
        public static MexProp AsProp(this MexItem item)
        {
            var prop = item as MexProp;
            if (prop == null)
                throw new MexException($"The item is not a {nameof(MexProp)}.");

            return prop;
        }

        public static MexValue AsValue(this MexItem item)
        {
            var value = item as MexValue;
            if (value == null)
                throw new MexException($"The item is not a {nameof(MexValue)}.");

            return value;
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexExpressionNode : MexItem
    {
        [DataMember]
        public MexOp Op { get; set; } = MexOp.None;

        [DataMember]
        public MexItem Left { get; set; }

        [DataMember]
        public MexItem Right { get; set; }

        public bool IsEmpty
        {
            get { return Left == null && Right == null; }
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexCondition : MexItem
    {
        [DataMember]
        public MexOp Op { get; set; }

        [DataMember]
        public MexItem Left { get; set; }

        [DataMember]
        public MexItem Right { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexOperation : MexItem
    {
        [DataMember]
        public MexOp Op { get; set; }

        [DataMember]
        public MexItem Left { get; set; }

        [DataMember]
        public MexItem Right { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexBinaryExpression : MexItem
    {
        [DataMember]
        public MexOp Op { get; set; }

        [DataMember]
        public MexItem Left { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MexCollection : MexItem
    {
        public MexCollection()
        {
            Items = new List<MexItem>();
        }

        [DataMember]
        public List<MexItem> Items { get; private set; }
    }
}