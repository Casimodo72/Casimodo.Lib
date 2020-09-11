using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public static class JsExtensions
    {
        public static string ToJs(this MojSortDirection direction)
        {
            return direction == MojSortDirection.Ascending ? "asc" : "desc";
        }
    }
}