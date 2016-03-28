using System.Web;
using System.Web.SessionState;

namespace Casimodo.Lib.Web
{
    public static class SessionExtensions
    {
        public static T Get<T>(this HttpSessionState session, string key)
        {
            if (session == null)
                return default(T);

            return (T)session[key];
        }

        public static void Set<T>(this HttpSessionState session, string key, T value)
        {
            session[key] = value;
        }

        public static T Get<T>(this HttpSessionStateBase session, string key)
        {
            if (session == null)
                return default(T);

            return (T)session[key];
        }

        public static void Set<T>(this HttpSessionStateBase session, string key, T value)
        {
            session[key] = value;
        }
    }
}