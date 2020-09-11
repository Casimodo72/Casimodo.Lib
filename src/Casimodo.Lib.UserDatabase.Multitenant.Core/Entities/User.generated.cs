using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Identity
{
    [TypeIdentity("7387b6ff-9681-4f0c-8801-99c3990a2a50")]
    [KeyInfo(PropName = "Id")]
    [TenantKeyInfo(PropName = "TenantId")]
    public partial class User
        : IKeyAccessor<Guid>, IKeyAccessor, IGuidGenerateable, IMultitenant
    {

        /// <summary>
        /// Indicates whether this is a non-human system user. E.g. a mail-sending machinery, etc.
        /// </summary>
        [Display(Name = "System")]
        [DefaultValue(value: false)]
        public bool IsSystem { get; set; }

        [NotMapped]
        [StringLength(maximumLength: 64)]
        public string Dummy { get; set; }

        Guid IKeyAccessor<Guid>.GetKey()
        {
            return Id;
        }

        object IKeyAccessor.GetKeyObject()
        {
            return Id;
        }

        void IGuidGenerateable.GenerateGuid()
        {
            if (Id == Guid.Empty) Id = Guid.NewGuid();
        }

        object IMultitenant.GetTenantKey()
        {
            return TenantId;
        }

        void IMultitenant.SetTenantKey(object tenantKey)
        {
            TenantId = (Guid?)tenantKey;
        }
    }
}
