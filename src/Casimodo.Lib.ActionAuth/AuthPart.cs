﻿using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Auth
{
    public class AuthPart
    {
        public string PartName { get; set; }
        public string PartGroup { get; set; }
        public List<AuthAction> Actions { get; private set; } = new List<AuthAction>();
        public List<AuthActionPerm> Permissions { get; private set; } = new List<AuthActionPerm>();
        public List<WebComponentRegItem> Components { get; set; } = new List<WebComponentRegItem>();

        public IEnumerable<AuthViewAction> GetViewActions()
        {
            return Actions.OfType<AuthViewAction>();
        }

        public bool GetIsDuplicate(AuthActionPerm perm)
        {
            return Permissions.Any(x =>
                x.Action == perm.Action &&
                x.UserRole == perm.UserRole &&
                x.IsPermitted == perm.IsPermitted &&
                x.IsMinRole == perm.IsMinRole);
        }

        public override string ToString()
        {
            return $"part:{PartName}, pgroup:{PartGroup}";
        }
    }
}
