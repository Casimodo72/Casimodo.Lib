namespace Casimodo.Lib.Mojen
{
    public class KendoFxConfig
    {
        public static readonly KendoFxConfig None = new(false);

        public KendoFxConfig(bool @is)
        {
            Is = @is;
        }

        public bool Is { get; set; }
        public string Effects { get; set; }
        public int Duration { get; set; } = 100;
    }
}
