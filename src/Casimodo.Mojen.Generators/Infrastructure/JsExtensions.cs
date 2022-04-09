namespace Casimodo.Mojen
{
    public static class JsExtensions
    {
        public static string ToJs(this MojSortDirection direction)
        {
            return direction == MojSortDirection.Ascending ? "asc" : "desc";
        }
    }
}