﻿namespace Casimodo.Mojen
{
    /// <summary>
    /// NOTE: Not intended to be serialized due to included MojFormedType.
    /// </summary>
    public class MojCascadeFromConfigCollection
    {
        public static readonly MojCascadeFromConfigCollection None = new(false);

        public MojCascadeFromConfigCollection()
            : this(true)
        { }

        MojCascadeFromConfigCollection(bool @is)
        {
            Is = @is;
        }

        public bool Is { get; private set; }

        public List<MojCascadeFromConfig> Items { get; set; } = [];
    }

    [Flags]
    public enum MojFilterCommandBehavior
    {
        None = 0,
        HideOnDeactivated = 1 << 0
    }

    public class MojCascadeFromConfig
    {
        // TODO: RENAME to CommandId
        public Guid? FilterId { get; set; }
        public MojFormedType FromType { get; set; }
        public string CommandTitle { get; set; }
        public string FromPropDisplayName { get; set; }
        public bool IsOptional { get; set; }
        public bool IsDeactivatable { get; set; }
        public MojFilterCommandBehavior CommandBehavior { get; set; } = MojFilterCommandBehavior.None;
        public string CommandGroup { get; set; }
    }
}