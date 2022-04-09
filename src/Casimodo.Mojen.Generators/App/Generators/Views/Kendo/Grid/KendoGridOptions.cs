namespace Casimodo.Mojen
{
    public class KendoGridOptions : ICloneable
    {
        public string CustomQueryBase { get; set; }
        
        // We need to use a custom OData method e.g. for querying of return Mos with IsDeleted and IsRecyclableDeleted.
        public string CustomQueryMethod { get; set; }

        public object Height { get; set; }

        public bool IsScrollable { get; set; }

        public int PageSize { get; set; } = 20;

        // TODO: REMOVE
        // public bool IsCreatable { get; set; } = true;

        // TODO: REMOVE
        // public bool? IsDeletable { get; set; }

        public bool IsHeaderVisible { get; set; } = true;

        /// <summary>
        /// KABU TODO: Not implemented yet. We need to use CSS for hiding the column header.
        /// </summary>
        public bool IsColumnHeaderVisible { get; set; } = true;

        public bool IsPagerVisible { get; set; } = true;

        public bool IsServerPaging { get; set; } = true;

        public KendoPagerOptions Pager { get; set; } = new KendoPagerOptions();

        public object Clone()
        {
            var clone = (KendoGridOptions)this.MemberwiseClone();
            clone.Pager = (KendoPagerOptions)Pager.Clone();

            return clone;
        }
    }
}