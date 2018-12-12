using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace Casimodo.Lib.Auth
{
    public class AuthRoleSetting
    {
        public string Name { get; set; }
        public string Permit { get; set; }
        public string Deny { get; set; }
    }

    public class UIComponentInfo
    {
        public string Id { get; set; }

        public string Part { get; set; }
        public string Group { get; set; }
        public string Role { get; set; }
        public string Actions { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }

        public List<AuthRoleSetting> AuthRoles { get; set; }

        public UIComponentInfo SetRole(string role, string permit, string deny)
        {
            Set(new AuthRoleSetting { Name = role, Permit = permit, Deny = deny });

            return this;
        }

        public UIComponentInfo Set(params AuthRoleSetting[] roles)
        {
            if (AuthRoles == null)
                AuthRoles = new List<AuthRoleSetting>();
            for (int i = 0; i < roles.Length; i++)
                AuthRoles.Add(roles[i]);

            return this;
        }

        public string GetUrlPath()
        {
            if (string.IsNullOrEmpty(Url))
                return null;

            return Url;

            // KABU TODO: REMOVE?
            //var path = Url;

            //if (path.StartsWith("/"))
            //    return path;
            //else
            //    return "/" + path;
        }

        public override string ToString()
        {
            return $"'{Part}' '{Group}' '{Role}'";
        }
    }

    // KABU TODO: Maybe load some auth parts from file (using HostingEnvironment.ApplicationPhysicalPath).
    public abstract class ActionAuthManager : IDisposable
    {
        internal readonly Dictionary<string, int> RoleInheritance = new Dictionary<string, int>();

        public ActionAuthManager()
        {
            RoleInheritance.Add("Admin", 100);
            RoleInheritance.Add("CoAdmin", 99);
            RoleInheritance.Add("Manager", 90);
            RoleInheritance.Add("Employee", 80);
            RoleInheritance.Add("ExternEmployee", 10);
        }

        public List<AuthPart> Parts { get; private set; } = new List<AuthPart>();

        public bool IsPermitted(IPrincipal user, string action, string part, string group, string vrole)
        {
            return GetMatchingPermissions(user, action, part, group, vrole).Any();
        }

        public IEnumerable<AuthActionPerm> GetMatchingPermissions(IPrincipal user, string action, string part, string group, string vrole)
        {
            // KABU TODO: IMPORTANT: Something's wrong here:
            //   We can't query for permissions for a specific view role.
            //   E.g. if the consumer ask for permission for a "Page" he might get
            //   the permission for "Lookup".
            var userRoles = user.Identity.GetUserRoles().ToArray();

            foreach (var p in Parts.Where(x => x.PartName == part && x.PartGroup == group))
                foreach (var perm in p.Permissions.Where(x => x.Action.Matches(action, vrole)))
                    for (int i = 0; i < userRoles.Length; i++)
                        if (perm.MatchesUserRole(userRoles[i]))
                            yield return perm;
        }

        protected ActionAuthBuilder GetBuilder()
        {
            return new ActionAuthBuilder(this);
        }



        internal void AuthRole(string role, string item, string vgroup = null, string vrole = "Page",
            string permit = CommonAuthVerb.View,
            string deny = null,
            bool minRole = true)
        {
            if (role == "AnyEmployee")
            {
                if (!minRole)
                    throw new Exception("Must not use 'AnyEmployee' role alias with non-min authorization.");

                role = "ExternEmployee";
            }

            var parts = Parts.Where(x =>
               x.PartName == item &&
               (vgroup == "*" || x.PartGroup == vgroup))
               .ToArray();

            if (!parts.Any())
                throw new Exception($"The part does not the exist.");

            foreach (var part in parts)
            {
                var permissionContext = ExpandVerbs(part, permit, deny);

                ConfigureAuth(part, permissionContext, viewRole: vrole, userRole: role);
            }
        }

        protected void ConfigureAuth(AuthPart part, EpandedVerbs verbs, string viewRole, string userRole, bool isMinUserRole = true)
        {
            foreach (var actionName in verbs.Permit)
            {
                foreach (var action in part.Actions.Where(x => x.Matches(actionName, viewRole)))
                {
                    var perm = new AuthActionPerm(this, part);
                    perm.Action = action;
                    perm.IsPermitted = true;
                    perm.UserRole = userRole;
                    perm.IsMinRole = isMinUserRole;

                    if (!part.GetIsDuplicate(perm))
                        part.Permissions.Add(perm);
                }
            }

            foreach (var actionName in verbs.Deny)
            {
                foreach (var action in part.Actions.Where(x => x.Matches(actionName, viewRole)))
                {
                    // Remove permissions of that action from that user role.
                    foreach (var perm in part.Permissions.Where(x =>
                        x.Action == action &&
                        x.UserRole == userRole)
                        .ToArray())
                        part.Permissions.Remove(perm);
                }
            }
        }

        public void Dispose()
        {
            // NOP
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        internal void CheckNotDuplicate(AuthPart part)
        {
            if (Parts.Any(x => x.PartName == part.PartName && x.PartGroup == part.PartGroup))
                throw new Exception($"Duplicate part (name: {part.PartName}, group: {part.PartGroup}).");
        }

        public class EpandedVerbs
        {
            public string[] Permit { get; set; }
            public string[] Deny { get; set; }
        }

        internal protected EpandedVerbs ExpandVerbs(AuthPart part, string permit, string deny)
        {
            var result = new EpandedVerbs();
            result.Deny = ExpandVerbsCore(part, deny, null);
            result.Permit = ExpandVerbsCore(part, permit, result.Deny);

            return result;
        }

        string[] ExpandVerbsCore(AuthPart part, string expression, string[] exclude)
        {
            if (string.IsNullOrEmpty(expression))
                return Array.Empty<string>();

            var items = expression.Split(",");

            List<string> result = null;
            string v;
            bool expanded = false;
            for (int i = 0; i < items.Length; i++)
            {
                v = items[i].Trim();
                if (v == "*")
                {
                    if (part == null)
                        throw new Exception("Cannot expand action wildcard '*' wihtout an AuthPart.");

                    if (expanded)
                        continue;

                    expanded = true;

                    for (int k = 0; k < part.Actions.Count; k++)
                        AddVerb(ref result, part.Actions[k].Name, exclude);

                }
                else
                {
                    AddVerb(ref result, v, exclude);
                }
            }

            return result?.ToArray() ?? Array.Empty<string>();
        }

        void AddVerb(ref List<string> target, string verb, string[] exclude)
        {
            if ((target == null || !target.Contains(verb)) &&
                (exclude == null || !exclude.Contains(verb)))
            {
                if (target == null)
                    target = new List<string>();
                target.Add(verb);
            }
        }
    }

    public class ActionAuthBuilder
    {
        ActionAuthManager _manager;

        public ActionAuthBuilder(ActionAuthManager manager)
        {
            _manager = manager;
        }

        public AuthPart CurrentPart { get; set; }

        public ActionAuthBuilder AuthRole(string role, string permit, string deny = null, string vrole = null, bool minRole = true)
        {
            _manager.AuthRole(role: role,
                item: CurrentPart.PartName,
                vgroup: CurrentPart.PartGroup,
                vrole: vrole,
                permit: permit,
                deny: deny,
                minRole: minRole);

            return this;
        }

        public ActionAuthBuilder AddActions(params string[] verbs)
        {
            foreach (var verb in verbs)
                AddViewAction(verb);

            return this;
        }

        public ActionAuthBuilder AddApiAction(string verb)
        {
            var permissionContext = _manager.ExpandVerbs(null, verb, null);
            foreach (var actionName in permissionContext.Permit)
            {
                if (!CurrentPart.Actions.Any(x => x.Matches(actionName)))
                {
                    CurrentPart.Actions.Add(new AuthApiAction
                    {
                        Name = actionName
                    });
                }
            }

            return this;
        }

        public ActionAuthBuilder AddViewAction(string verb)
        {
            var permissionContext = _manager.ExpandVerbs(null, verb, null);
            foreach (var actionName in permissionContext.Permit)
            {
                foreach (var entry in CurrentPart.Components)
                {
                    if (!CurrentPart.Actions.Any(x => x.Matches(actionName, entry.Role)))
                    {
                        CurrentPart.Actions.Add(new AuthViewAction
                        {
                            Name = actionName,
                            ViewRole = entry.Role,
                            ViewUrl = entry.Url
                        });
                    }
                }
            }

            return this;
        }

        public ActionAuthBuilder AddPart(string item, string title = null, string group = null, string viewRole = null, string url = null)
        {
            var part = new AuthPart();
            part.PartName = item;
            part.PartGroup = group;

            _manager.CheckNotDuplicate(part);

            CurrentPart = part;

            part.Components.Add(new UIComponentInfo
            {
                Part = item,
                Title = title,
                Url = url,
                Role = viewRole
            });

            _manager.Parts.Add(part);

            return this;
        }

        public ActionAuthBuilder GetOrAddPart(string item, string group = null)
        {
            var part = _manager.Parts.FirstOrDefault(x => x.PartName == item && x.PartGroup == group);

            if (part == null)
            {
                part = new AuthPart();
                part.PartName = item;
                part.PartGroup = group;
                _manager.Parts.Add(part);
            }

            CurrentPart = part;

            return this;
        }

        public ActionAuthBuilder GetPart(string item, string group = null)
        {
            var part = _manager.Parts.SingleOrDefault(x => x.PartName == item && x.PartGroup == group);

            if (part == null)
                throw new Exception($"Part '{item}' not found.");

            CurrentPart = part;

            return this;
        }

        public ActionAuthBuilder AddPage(string part, string title, string url, string group = null)
        {
            if (!url.StartsWith("/"))
                url = "/" + url;

            var curpart = CurrentPart = new AuthPart();
            curpart.PartName = part;
            curpart.PartGroup = group;
            curpart.Actions.Add(new AuthViewAction
            {
                Name = CommonAuthVerb.View,
                ViewRole = "Page",
                ViewUrl = url
            });
            curpart.Components.Add(new UIComponentInfo
            {
                Part = part,
                Role = "Page",
                Title = title,
                Url = url,
            });

            _manager.Parts.Add(curpart);

            return this;
        }

        public ActionAuthBuilder List(string url)
        {
            return AddAction(CommonAuthVerb.View, "List", url);
        }

        public ActionAuthBuilder AddAction(string name, string vrole, string url)
        {
            if (!url.StartsWith("/"))
                url = "/" + url;

            CurrentPart.Actions.Add(new AuthViewAction
            {
                Name = name,
                ViewRole = vrole,
                ViewUrl = url
            });

            return this;
        }
    }
}
