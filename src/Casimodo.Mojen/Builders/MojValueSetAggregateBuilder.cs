namespace Casimodo.Mojen
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
                    string.Format("Duplicate aggregate value names (aggregate name: '{0}', duplicates: {1})",
                        Aggregate.Name,
                        aggNameDupls.Select(x => $"'{x}'").Join(", ")));
        }
    }
}