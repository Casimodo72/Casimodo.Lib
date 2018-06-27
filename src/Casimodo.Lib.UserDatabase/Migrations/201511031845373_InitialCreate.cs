namespace Casimodo.Lib.Identity.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AuthRoles",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Index = c.Int(nullable: false),
                        DisplayName = c.String(maxLength: 64),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "UIX_RoleNameIndex");
            
            CreateTable(
                "dbo.AuthUserRoles",
                c => new
                    {
                        UserId = c.Guid(nullable: false),
                        RoleId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AuthRoles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.AuthUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.AuthUsers",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        TenantId = c.Guid(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        IsSystem = c.Boolean(nullable: false),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => new { t.TenantId, t.UserName }, unique: true, name: "UIX_UserNameIndex");
            
            CreateTable(
                "dbo.AuthUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Guid(nullable: false),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AuthUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AuthUserLogins",
                c => new
                    {
                        TenantId = c.Guid(nullable: false),
                        UserId = c.Guid(nullable: false),
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.TenantId, t.UserId, t.LoginProvider, t.ProviderKey })
                .ForeignKey("dbo.AuthUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AuthUserRoles", "UserId", "dbo.AuthUsers");
            DropForeignKey("dbo.AuthUserLogins", "UserId", "dbo.AuthUsers");
            DropForeignKey("dbo.AuthUserClaims", "UserId", "dbo.AuthUsers");
            DropForeignKey("dbo.AuthUserRoles", "RoleId", "dbo.AuthRoles");
            DropIndex("dbo.AuthUserLogins", new[] { "UserId" });
            DropIndex("dbo.AuthUserClaims", new[] { "UserId" });
            DropIndex("dbo.AuthUsers", "UIX_UserNameIndex");
            DropIndex("dbo.AuthUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AuthUserRoles", new[] { "UserId" });
            DropIndex("dbo.AuthRoles", "UIX_RoleNameIndex");
            DropTable("dbo.AuthUserLogins");
            DropTable("dbo.AuthUserClaims");
            DropTable("dbo.AuthUsers");
            DropTable("dbo.AuthUserRoles");
            DropTable("dbo.AuthRoles");
        }
    }
}
