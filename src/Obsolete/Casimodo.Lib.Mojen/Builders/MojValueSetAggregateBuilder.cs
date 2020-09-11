using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class MojValueSetAggregateBuilder
    {
        public MojValueSetAggregateBuilder(string name)
        {
            Aggregate = new MojValueSetAggregate { Name = name };
        }

        public MojValueSetAggregate Aggregate { get; private set; }

        public MojValueSetAggregateBuilder Add(params string[] valueNames)
        {
            Aggregate.AddRange(valueNames);
            return this;
        }

        public MojValueSetAggregateBuilder Description(string description)
        {
            Aggregate.Description = description;
            return this;
        }

        public void Validate()
        {
            var aggNameDupls = Aggregate.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
            if (aggNameDupls.Count != 0)
                throw new MojenException(
                    string.Format("Duplicate aggregate value names (aggregate {0}: {1})",
                        Aggregate.Name,
                        aggNameDupls.Join(", ")));
        }
    }
}