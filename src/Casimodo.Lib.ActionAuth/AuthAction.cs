namespace Casimodo.Lib.Auth
{
    public sealed class AuthApiAction : AuthAction
    {
        public override bool Matches(string name, string vrole = null)
        {
            return name == Name && (vrole == null || vrole == "*");
        }
    }

    public abstract class AuthAction
    {
        public string Name { get; set; }

        public abstract bool Matches(string name, string vrole = null);

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
