using Dapper;
using System.Data.SqlClient;
using System.IO;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Reads database data of an entity and transforms that data to a Mojen DB seed definition.
    /// </summary>
    public class EntityUserFromDbToSeedGen : EntityFromDbTransformationGenBase
    {
        public EntityUserFromDbToSeedOptions UserOptions { get; set; }

        public override void GenerateExport()
        {
            foreach (var item in App.GetItems<MojValueSetContainer>().Where(x => x.Uses(this)))
            {
                if (item.TypeConfig.Name != "User")
                    throw new MojenException("This generator is intended for types of name 'User' only.");

                Options = item.GetGeneratorConfig<EntityFromDbTransformationOptions>();
                if (Options?.IsEnabled == false)
                    continue;

                UserOptions = Options as EntityUserFromDbToSeedOptions ?? new EntityUserFromDbToSeedOptions();

                string outputDirPath = Options?.OutputDirPath ?? MainSeedConfig.DbImportOutputSeedDirPath;

                var filePath = Path.Combine(outputDirPath, item.TypeConfig.Name + ".Seed.generated.cs");

                PerformWrite(filePath, () => GenerateExport(item));
            }
        }

        class AuthUserRole
        {
            public Guid UserId { get; set; }
            public Guid RoleId { get; set; }
        }

        class AuthRole
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        class UserInfoItem
        {
            public Guid Id { get; set; }
            public string UserName { get; set; }
            public string Pw { get; set; }
            public string Ph { get; set; }
        }

        List<UserInfoItem> ReadPwSource()
        {
            var items = new List<UserInfoItem>();

            if (string.IsNullOrWhiteSpace(UserOptions.PwSourceFilePath))
                return items;

            var doc = XDocument.Load(UserOptions.PwSourceFilePath);
            foreach (var userElem in doc.Element("Users").Elements("User"))
            {
                items.Add(new UserInfoItem
                {
                    Id = (Guid)userElem.Attr("Id"),
                    UserName = (string)userElem.Attr("Name"),
                    Pw = (string)userElem.Attr("PW"),
                    Ph = (string)userElem.Attr("PH")
                });
            }

            return items;
        }

        bool TryGetPwSourceValue(string propName, UserInfoItem item, out object value)
        {
            value = null;
            if (item == null)
                return false;

            if (propName == "PasswordHash")
                value = item.Ph;
            else
                return false;

            return true;
        }

        public void GenerateExport(MojValueSetContainer container)
        {
            var seedType = container.TypeConfig;
            var storeType = seedType.GetNearestStore();

            var pwsource = ReadPwSource();

            ONamespace("Casimodo.Lib.Mojen");

            O($"public partial class {storeType.Name}Seed : MojGeneratedDbSeed");
            Begin();

            // Constructor
            O($"public override void PopulateCore(MojValueSetContainerBuilder seed)");
            Begin();

            var dbprops = container.GetSeedableProps().Select(x => x.StoreOrSelf).ToArray();

            if (dbprops.Any(x => x.Type.Type == null))
                throw new MojenException("Deed definition must not contain non simple type properties.");

            var dbFields = dbprops.Select(x => "[" + container.GetImportPropName(x.Name) + "]").Join(", ");

            var dbTable = storeType.TableName;

            var query = $"select {dbFields} from [{dbTable}]";

            // Sort
            query = AddOrderBy(query);

            Type queryType = Moj.CreateType(storeType, dbprops);

            using (var conn = new SqlConnection(MainSeedConfig.DbImportConnectionString))
            {
                var allUserToRole = conn.Query<AuthUserRole>("select UserId, RoleId from AuthUserRoles").ToArray();
                var allRoles = conn.Query<AuthRole>("select Id, Name from AuthRoles").Cast<AuthRole>().ToArray();

                foreach (var entity in conn.Query(queryType, query))
                {
                    Guid id = (Guid)Casimodo.Lib.TypeHelper.GetTypeProperty(entity, "Id", required: true).GetValue(entity);

                    var pwitem = pwsource.FirstOrDefault(x => x.Id == id);

                    Oo("seed.Add(");

                    int i = 0;
                    string userName = "";
                    foreach (var prop in dbprops)
                    {

                        if (!TryGetPwSourceValue(prop.Name, pwitem, out object value))
                        {
                            value = Casimodo.Lib.TypeHelper.GetTypeProperty(entity, prop.Name, required: true)
                                .GetValue(entity);
                        }

                        if (prop.Name == "UserName")
                            userName = value as string;

                        if (value == null)
                        {
                            o("null");
                        }
                        else if (prop.Type.Type == typeof(string))
                        {
                            o("\"" + ((string)value).Replace("\"", @"\""") + "\"");
                        }
                        else if (prop.Type.TypeNormalized == typeof(Guid))
                        {
                            o("\"" + value.ToString() + "\"");
                        }
                        else if (prop.Type.IsEnum)
                        {
                            // KABU TODO: IMPORTANT: Handle enums.
                            o(Moj.CS(value, parse: false));
                        }
                        else
                        {
                            o(Moj.CS(value, parse: false));
                        }

                        if (++i < dbprops.Length)
                            o(", ");
                    }

                    o(")");

                    // Roles
                    var roles = allUserToRole
                        .Where(x => x.UserId == id)
                        .SelectMany(x => allRoles.Where(role => role.Id == x.RoleId))
                        .Select(x => x.Name)
                        .Join(",");

                    if (!string.IsNullOrEmpty(roles))
                    {
                        o($".AuthRoles(\"{roles}\")");
                    }

                    if (pwitem != null)
                    {
                        o($".Pw(\"{pwitem.Pw}\")");
                    }

                    oO($"; // {userName}");
                }
            }

            End();
            End();
            End();
        }
    }

    ///// <summary>
    ///// Source: https://github.com/dotnet/efcore/issues/1862
    ///// </summary>
    //public static class SqlQueryExtensions
    //{
    //    public static IList<T> SqlQuery<T>(this DbContext db, string sql, params object[] parameters) where T : class
    //    {
    //        using (var db2 = new ContextForQueryType<T>(db.Database.GetDbConnection()))
    //        {
    //            return db2.Set<T>().FromSqlRaw(sql, parameters).ToList();
    //        }
    //    }

    //    private class ContextForQueryType<T> : DbContext where T : class
    //    {
    //        private readonly DbConnection connection;

    //        public ContextForQueryType(DbConnection connection)
    //        {
    //            this.connection = connection;
    //        }

    //        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //        {
    //            optionsBuilder.UseSqlServer(connection, options => options.EnableRetryOnFailure());

    //            base.OnConfiguring(optionsBuilder);
    //        }

    //        protected override void OnModelCreating(ModelBuilder modelBuilder)
    //        {
    //            modelBuilder.Entity<T>().HasNoKey();
    //            base.OnModelCreating(modelBuilder);
    //        }
    //    }
    //}

}