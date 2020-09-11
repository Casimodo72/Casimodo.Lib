using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib
{
#if (!WINDOWS_UWP)
    [Serializable]
#endif
    public class CasimodoLibException : Exception
    {
        public CasimodoLibException() { }
        public CasimodoLibException(string message) : base(message) { }
        public CasimodoLibException(string message, Exception inner) : base(message, inner) { }

#if (!WINDOWS_UWP)
        protected CasimodoLibException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
#endif
    }
}
