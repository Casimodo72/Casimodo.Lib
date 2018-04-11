namespace Casimodo.Lib.Auth
{
    public abstract class AuthAction
    {
        public string Name { get; set; }

        public virtual bool Matches(string name, string vrole = null)
        {
            return vrole == null && name == Name;
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
