using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public static class JsExtensions
    {
        public static string ToJs(this MojOrderDirection direction)
        {
            return direction == MojOrderDirection.Ascending ? "asc" : "desc";
        }
    }
}