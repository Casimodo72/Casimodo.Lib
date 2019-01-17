using System.Collections.Generic;

namespace Casimodo.Lib.Auth
{
    public class AuthActionPerm
    {
        internal AuthActionPerm(ActionAuthManager manager, AuthPart part)
        {
            _manager = manager;
            Part = part;
        }
        internal ActionAuthManager _manager;

        public string UserRole { get; set; }
        public bool IsMinRole { get; set; }

        public bool IsPermitted { get; set; }

        public AuthPart Part { get; set; }

        public AuthAction Action { get; set; }

        public override string ToString()
        {
            return $"{(IsPermitted ? "perm" : "deny")} urole:'{UserRole}' -> action:({Action})";
        }

        public bool MatchesUserRole(string userRole)
        {
            if (IsMinRole)
                return _manager.RoleInheritance[userRole] >= _manager.RoleInheritance[UserRole];
            else
                return UserRole == userRole;
        }

        public bool MatchesUserRoles(IEnumerable<string> userRoles)
        {
            foreach (var userRole in userRoles)
            {
                var matches = MatchesUserRole(userRole);
                if (matches)
                    return true;
            }

            return false;
        }
    }
}
