namespace Casimodo.Lib.Mojen
{
    public class EntityToDbTransformationOptions : MojBase
    {
        public bool IsEnabled { get; set; }
        public string DbConnectionString { get; set; }
        public string InputDirPath { get; set; }
    }
}