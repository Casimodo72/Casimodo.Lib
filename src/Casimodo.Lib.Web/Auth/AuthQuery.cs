using System.Collections.Generic;

namespace Casimodo.Lib.Web.Auth
{

    public class AuthPartQuery
    {
        public string Part { get; set; }
        public string Group { get; set; }
        public string VRole { get; set; }
    }

    public class AuthPartQueryResult
    {
        public string Part { get; set; }
        public string Group { get; set; }

        public List<AuthPermissionQueryResult> Permissions { get; set; } = [];

        public override string ToString()
        {
            return $"{Part} group:{Group}";
        }
    }

    public class AuthPermissionQueryResult
    {
        public string VRole { get; set; }
        public string Action { get; set; }

        public override string ToString()
        {
            return $"{Action} vrole:{VRole}";
        }
    }

    public class AuthPartQueryResultContainer
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string[] UserRoles { get; set; }

        public List<AuthPartQueryResult> Items { get; set; } = [];
    }
}
