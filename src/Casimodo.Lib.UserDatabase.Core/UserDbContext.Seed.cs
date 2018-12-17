using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Casimodo.Lib.Identity
{
    public partial class UserDbContext
    {
        // Seed: https://docs.microsoft.com/en-us/ef/core/modeling/data-seeding

        // Adjust to your needs.
        void OnModelCreatingSeed(ModelBuilder builder)
        {
            builder.Entity<Role>().HasData(
                new Role
                {
                    Index = 1,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    DisplayName = "Administrator",
                    Id = Guid.Parse("14eea27c-697f-4494-9413-bcdd47611c20")
                },
                new Role
                {
                    Index = 2,
                    Name = "CoAdmin",
                    NormalizedName = "COADMIN",
                    DisplayName = "Co-Administrator",
                    Id = Guid.Parse("d40c9767-596e-44da-9698-56b177ec17d6")
                },
                new Role
                {
                    Index = 2,
                    Name = "Manager",
                    NormalizedName = "MANAGER",
                    DisplayName = "Manager",
                    Id = Guid.Parse("b8c9137f-9cff-4a2e-a2fc-ef82d0f837c5")
                },
                new Role
                {
                    Index = 100,
                    Name = "AnyEmployee",
                    NormalizedName = "ANYEMPLOYEE",
                    DisplayName = "Mitarbeiter (eigener/externer/fremder)",
                    Id = Guid.Parse("09a0692b-126e-4a2e-bbad-ee0081dd3d47")
                },
                new Role
                {
                    Index = 3,
                    Name = "Employee",
                    NormalizedName = "EMPLOYEE",
                    DisplayName = "Mitarbeiter (eigener)",
                    Id = Guid.Parse("77bd192e-db17-4a6c-ab4e-29ed53b72c7a")
                },
                new Role
                {
                    Index = 4,
                    Name = "ExternEmployee",
                    NormalizedName = "EXTERNEMPLOYEE",
                    DisplayName = "Mitarbeiter (externer)",
                    Id = Guid.Parse("9b9a7457-0239-437e-b5c3-03065894d329")
                },
                new Role
                {
                    Index = 5,
                    Name = "ForeignEmployee",
                    NormalizedName = "FOREIGNEMPLOYEE",
                    DisplayName = "Mitarbeiter (fremder)",
                    Id = Guid.Parse("5179808a-afb5-479c-b482-b3663277cbd0")
                },
                new Role
                {
                    Index = 6,
                    Name = "Customer",
                    NormalizedName = "CUSTOMER",
                    DisplayName = "Kunde",
                    Id = Guid.Parse("ac26cbf0-473e-453f-8c65-e3e5fe26f0d6")
                });
        }
    }
}