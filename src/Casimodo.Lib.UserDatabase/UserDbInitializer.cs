using System.Data.Entity;

namespace Casimodo.Lib.Identity
{
    // DropCreateDatabaseAlways<UserDbContext>
    // CreateDatabaseIfNotExists<UserDbContext>
    // NullDatabaseInitializer<UserDbContext>

    public partial class UserDbInitializer : NullDatabaseInitializer<UserDbContext>
    { }
}