namespace Casimodo.Lib.Mojen
{
    public class EntityFromDbTransformationOptions : MojBase
    {
        public bool IsEnabled { get; set; } = true;
        public string OutputDirPath { get; set; }

        public string OrderBy { get; set; }

        /// KABU TODO: Not implemented yet
        //public string Filter { get; set; }
    }
}