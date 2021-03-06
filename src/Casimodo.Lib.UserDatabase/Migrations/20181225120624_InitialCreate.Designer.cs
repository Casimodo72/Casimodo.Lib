﻿// <auto-generated />
using System;
using Casimodo.Lib.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Casimodo.Lib.UserDatabase.Migrations
{
    [DbContext(typeof(UserDbContext))]
    [Migration("20181225120624_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Casimodo.Lib.Identity.Role", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("DisplayName")
                        .HasMaxLength(64);

                    b.Property<int>("Index");

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AuthRoles");

                    b.HasData(
                        new { Id = new Guid("14eea27c-697f-4494-9413-bcdd47611c20"), ConcurrencyStamp = "4469db95-6db4-4f66-b214-277dbf7adcb6", DisplayName = "Administrator", Index = 1, Name = "Admin", NormalizedName = "ADMIN" },
                        new { Id = new Guid("d40c9767-596e-44da-9698-56b177ec17d6"), ConcurrencyStamp = "ee8b0da3-09d0-480b-b3e3-2bd6e4cc5a75", DisplayName = "Co-Administrator", Index = 2, Name = "CoAdmin", NormalizedName = "COADMIN" },
                        new { Id = new Guid("b8c9137f-9cff-4a2e-a2fc-ef82d0f837c5"), ConcurrencyStamp = "618a2fcd-d601-48b9-b0ed-d80be340bc5b", DisplayName = "Manager", Index = 2, Name = "Manager", NormalizedName = "MANAGER" },
                        new { Id = new Guid("09a0692b-126e-4a2e-bbad-ee0081dd3d47"), ConcurrencyStamp = "44583ab9-ad4c-4cb7-a594-cc93b3b916d9", DisplayName = "Mitarbeiter (eigener/externer/fremder)", Index = 100, Name = "AnyEmployee", NormalizedName = "ANYEMPLOYEE" },
                        new { Id = new Guid("77bd192e-db17-4a6c-ab4e-29ed53b72c7a"), ConcurrencyStamp = "ea82d5c2-24d4-43e4-9f4a-397ee9852396", DisplayName = "Mitarbeiter (eigener)", Index = 3, Name = "Employee", NormalizedName = "EMPLOYEE" },
                        new { Id = new Guid("9b9a7457-0239-437e-b5c3-03065894d329"), ConcurrencyStamp = "517cd0e5-468c-46c4-bb7d-1e33933ae669", DisplayName = "Mitarbeiter (externer)", Index = 4, Name = "ExternEmployee", NormalizedName = "EXTERNEMPLOYEE" },
                        new { Id = new Guid("5179808a-afb5-479c-b482-b3663277cbd0"), ConcurrencyStamp = "72361ecf-6fcc-4a75-9d53-8cb22ae9684e", DisplayName = "Mitarbeiter (fremder)", Index = 5, Name = "ForeignEmployee", NormalizedName = "FOREIGNEMPLOYEE" },
                        new { Id = new Guid("ac26cbf0-473e-453f-8c65-e3e5fe26f0d6"), ConcurrencyStamp = "1ba859ac-b081-4c34-8920-e07ffd67f174", DisplayName = "Kunde", Index = 6, Name = "Customer", NormalizedName = "CUSTOMER" }
                    );
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.RoleClaim", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<Guid>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AuthRoleClaims");
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<bool>("IsDeleted");

                    b.Property<bool>("IsSystem");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AuthUsers");
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.UserClaim", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<Guid>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AuthUserClaims");
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.UserLogin", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<Guid>("UserId");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AuthUserLogins");
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.UserRole", b =>
                {
                    b.Property<Guid>("UserId");

                    b.Property<Guid>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasAlternateKey("RoleId", "UserId");

                    b.ToTable("AuthUserRoles");
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.UserToken", b =>
                {
                    b.Property<Guid>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AuthUserTokens");
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.RoleClaim", b =>
                {
                    b.HasOne("Casimodo.Lib.Identity.Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.UserClaim", b =>
                {
                    b.HasOne("Casimodo.Lib.Identity.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.UserLogin", b =>
                {
                    b.HasOne("Casimodo.Lib.Identity.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.UserRole", b =>
                {
                    b.HasOne("Casimodo.Lib.Identity.Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Casimodo.Lib.Identity.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Casimodo.Lib.Identity.UserToken", b =>
                {
                    b.HasOne("Casimodo.Lib.Identity.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
