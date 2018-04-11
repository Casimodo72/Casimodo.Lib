namespace Casimodo.Lib.Auth
{
    public sealed class AuthViewAction : AuthAction
    {
        public string ViewUrl { get; set; }
        public string ViewRole { get; set; }

        public override bool Matches(string name, string vrole = null)
        {
            return (name == "*" || name == Name) && (vrole == "*" || vrole == ViewRole);
        }

        public override string ToString()
        {
            return $"{Name} vrole:{ViewRole} url:{ViewUrl}";
        }
    }
}
