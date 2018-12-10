using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Casimodo.Lib;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Reads database data of an entity and transforms that data to a Mojen DB seed definition.
    /// </summary>
    public class EntityDbUserToSeedExporterGen : EntityExporterGenBase
    {
        public AuthUserEntityExporterOptions AuthUserOptions { get; set; }

        public override void GenerateExport()
        {
            foreach (var item in App.GetItems<MojValueSetContainer>().Where(x => x.Uses(this)))
            {
                if (item.TargetType.Name != "User")
                    throw new MojenException("This generator is intended for types of name 'User' only.");

                Options = item.GetGeneratorConfig<EntityExporterOptions>();
                if (Options?.IsEnabled == false)
                    continue;

                AuthUserOptions = Options as AuthUserEntityExporterOptions;

                string outputDirPath = Options?.OutputDirPath ?? ExportConfig.SourceDbDataFetchOutputDirPath;

                var filePath = Path.Combine(outputDirPath, item.TargetType.Name + ".Seed.generated.cs");

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

            if (AuthUserOptions == null || string.IsNullOrWhiteSpace(AuthUserOptions.PwSourceFilePath))
                return items;

            var doc = XDocument.Load(AuthUserOptions.PwSourceFilePath);
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
            var seedType = container.TargetType;
            var storeType = seedType.GetNearestStore();

            var pwsource = ReadPwSource();

            ONamespace("Casimodo.Lib.Mojen");

            O($"public partial class {storeType.Name}Seed");
            Begin();

            // Constructor
            O($"public void Populate(MojValueSetContainerBuilder seed)");
            Begin();

            var props = container.GetProps(defaults: false)
                //.Where(x => x.Type.Type != null)
                .Select(x => seedType.FindStoreProp(x.Name))
                .ToArray();

            if (props.Any(x => x.Type.Type == null))
                throw new MojenException("Deed definition must not contain non simple type properties.");

            var dbFields = props.Select(x => "[" + container.GetImportPropName(x.Name) + "]").Join(", ");

            var dbTable = storeType.TableName;

            var query = $"select {dbFields} from [{dbTable}]";

            // Sort
            if (!string.IsNullOrWhiteSpace(Options.OrderBy))
            {
                query += $" order by [{Options.OrderBy}]";
            }

            Type queryType = MojenUtils.CreateType(storeType, props);

            using (var db = new DbContext(ExportConfig.SourceDbConnectionString))
            {
                var allUserToRole = db.Database.SqlQuery(typeof(AuthUserRole), "select UserId, RoleId from AuthUserRoles").Cast<AuthUserRole>().ToArray();
                var allRoles = db.Database.SqlQuery(typeof(AuthRole), "select Id, Name from AuthRoles").Cast<AuthRole>().ToArray();

                foreach (var entity in db.Database.SqlQuery(queryType, query))
                {
                    Guid id = (Guid)Casimodo.Lib.TypeHelper.GetTypeProperty(entity, "Id", required: true).GetValue(entity);

                    var pwitem = pwsource.FirstOrDefault(x => x.Id == id);

                    Oo("seed.Add(");

                    int i = 0;
                    string userName = "";
                    foreach (var prop in props)
                    {
                        object value = null;

                        if (!TryGetPwSourceValue(prop.Name, pwitem, out value))
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
                            o(MojenUtils.ToCsValue(value, parse: false));
                        }
                        else
                        {
                            o(MojenUtils.ToCsValue(value, parse: false));
                        }

                        if (++i < props.Length)
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
}