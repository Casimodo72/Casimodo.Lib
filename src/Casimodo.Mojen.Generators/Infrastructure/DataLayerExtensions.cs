namespace Casimodo.Lib.Mojen
{
    public static class DataLayerExtensions
    {
        public static string GetTypeKeysClassName(this DataLayerConfig config)
        {
            return $"{config.TypePrefix ?? ""}TypeKeys";
        }
    }
}
