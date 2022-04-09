namespace Casimodo.Mojen
{
    public class MojTypeComparisonBuilder
    {
        public MojTypeComparisonBuilder()
        {
            Config = new MojTypeComparison();
        }

        public MojTypeComparison Config { get; private set; }

        public MojTypeComparisonBuilder Exclude(params string[] propNames)
        {
            if (propNames == null || propNames.Length == 0)
                return this;

            Config.ExcludedProps.AddRange(propNames);

            return this;
        }

        public MojTypeComparisonBuilder Include(params string[] propNames)
        {
            if (propNames == null || propNames.Length == 0)
                return this;

            Config.IncludedProps.AddRange(propNames);

            return this;
        }

        public MojTypeComparisonBuilder All()
        {
            Config.Mode = "all";
            return this;
        }

        public MojTypeComparisonBuilder None()
        {
            Config.Mode = "none";
            return this;
        }

        public MojTypeComparisonBuilder UseNonStored()
        {
            Config.UseNonStoredProps = true;
            return this;
        }

        public MojTypeComparisonBuilder UseNavigation()
        {
            Config.UseNavitationProps = true;
            return this;
        }

        public MojTypeComparisonBuilder UseList()
        {
            Config.UseListProps = false;
            return this;
        }
    }
}