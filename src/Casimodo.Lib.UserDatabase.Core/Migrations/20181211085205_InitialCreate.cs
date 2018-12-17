using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Casimodo.Lib.UserDatabase.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Index = table.Column<int>(nullable: false),
                    DisplayName = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: false),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: false),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    TenantId = table.Column<Guid>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false),
                    IsSystem = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<Guid>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthRoleClaims_AuthRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AuthRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthUserClaims_AuthUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AuthUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<Guid>(nullable: false),
                    TenantId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthUserLogins", x => new { x.TenantId, x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AuthUserLogins_AuthUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AuthUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    RoleId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthUserRoles", x => new { x.UserId, x.RoleId });
                    table.UniqueConstraint("AK_AuthUserRoles_RoleId_UserId", x => new { x.RoleId, x.UserId });
                    table.ForeignKey(
                        name: "FK_AuthUserRoles_AuthRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AuthRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthUserRoles_AuthUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AuthUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true),
                    Id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.UniqueConstraint("AK_AuthUserTokens_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthUserTokens_AuthUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AuthUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AuthRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "DisplayName", "Index", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("14eea27c-697f-4494-9413-bcdd47611c20"), "ce7293f7-e970-4c6e-8be4-4942a9e61fa9", "Administrator", 1, "Admin", "ADMIN" },
                    { new Guid("d40c9767-596e-44da-9698-56b177ec17d6"), "6acd91a6-9a2f-458d-8e57-3eb03a056d70", "Co-Administrator", 2, "CoAdmin", "COADMIN" },
                    { new Guid("b8c9137f-9cff-4a2e-a2fc-ef82d0f837c5"), "d3ea534b-7424-4009-bab5-e424ecd6b371", "Manager", 2, "Manager", "MANAGER" },
                    { new Guid("09a0692b-126e-4a2e-bbad-ee0081dd3d47"), "b4473b77-dc38-4827-a070-24f67457b136", "Mitarbeiter (eigener/externer/fremder)", 100, "AnyEmployee", "ANYEMPLOYEE" },
                    { new Guid("77bd192e-db17-4a6c-ab4e-29ed53b72c7a"), "c17eaad4-6cb0-4dc1-99f8-06816d2db847", "Mitarbeiter (eigener)", 3, "Employee", "EMPLOYEE" },
                    { new Guid("9b9a7457-0239-437e-b5c3-03065894d329"), "1c747e8a-6e51-4620-9a1a-0b8a7d265adf", "Mitarbeiter (externer)", 4, "ExternEmployee", "EXTERNEMPLOYEE" },
                    { new Guid("5179808a-afb5-479c-b482-b3663277cbd0"), "99d0a80d-ed8a-46b2-9777-654c5eaa774e", "Mitarbeiter (fremder)", 5, "ForeignEmployee", "FOREIGNEMPLOYEE" },
                    { new Guid("ac26cbf0-473e-453f-8c65-e3e5fe26f0d6"), "c01876ea-31b4-416b-a6a3-e4814e04ee99", "Kunde", 6, "Customer", "CUSTOMER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthRoleClaims_RoleId",
                table: "AuthRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UIX_NormalizedName",
                table: "AuthRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthUserClaims_UserId",
                table: "AuthUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthUserLogins_UserId",
                table: "AuthUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UIX_NormalizedEmail",
                table: "AuthUsers",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UIX_NormalizedUserName",
                table: "AuthUsers",
                columns: new[] { "TenantId", "NormalizedUserName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthRoleClaims");

            migrationBuilder.DropTable(
                name: "AuthUserClaims");

            migrationBuilder.DropTable(
                name: "AuthUserLogins");

            migrationBuilder.DropTable(
                name: "AuthUserRoles");

            migrationBuilder.DropTable(
                name: "AuthUserTokens");

            migrationBuilder.DropTable(
                name: "AuthRoles");

            migrationBuilder.DropTable(
                name: "AuthUsers");
        }
    }
}
