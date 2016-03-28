using Dynamitey;
using System;

namespace Casimodo.Lib
{
    /// <summary>
    /// Dynamic argument helper.
    /// </summary>
    public static class HArg
    {
        public static dynamic O()
        {
            return (dynamic)Builder.New();
        }

        public static object O(Action<dynamic> build)
        {
            var obj = (dynamic)Builder.New();
            build(obj);
            return (object)obj;
        }
    }
}