﻿namespace Casimodo.Lib.Mojen
{
    public enum MojDataSetSizeKind
    {
        ExtraSmall = 0,
        Small = 1,
        Normal = 2,
        Large = 3,
        ExtraLarge = 4
    }

    public class MojInterface
    {
        public string Name { get; set; }

        public bool AddToStore { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}