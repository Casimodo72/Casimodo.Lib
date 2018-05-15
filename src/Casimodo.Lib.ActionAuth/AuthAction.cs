namespace Casimodo.Lib.Auth
{
    public class AuthAction
    {
        public string Name { get; set; }

        public virtual bool Matches(string name, string vrole = null)
        {
            return name == Name && (vrole == null || vrole == "*");
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
