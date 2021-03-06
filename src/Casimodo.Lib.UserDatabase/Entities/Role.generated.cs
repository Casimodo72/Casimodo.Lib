﻿using Casimodo.Lib.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace Casimodo.Lib.Identity
{
    [TypeIdentity("be6d9274-a1cd-4bd3-ab0d-928e925c9539")]
    [KeyInfo(PropName = "Id")]
    public partial class Role
        : IKeyAccessor<Guid>, IKeyAccessor, IGuidGenerateable
    {

        /// <summary>
        /// Sort order.
        /// </summary>
        public int Index { get; set; }

        [StringLength(maximumLength: 64)]
        [Display(Name = "Name (Deutsch)")]
        public string DisplayName { get; set; }

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
    }
}
