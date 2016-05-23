using Casimodo.Lib.Data;
using System;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DbRepoCoreOnSavingMisc : DbRepoCoreGenBase
    {
        public DbRepoCoreOnSavingMisc()
        {
            Scope = "DataContext";

            Name = "OnSaving.Misc";

            AnyTypeMethodCall = (o) => $"void OnSavingMiscAny({o.DataConfig.DbRepoOperationContextName} ctx)";
            TypeMethodCall = (o, type) => $"OnSavingMisc(ctx.Item as {type.ClassName}, ctx);";
            TypeMethod = (o, type, item) => $"bool OnSavingMisc({type.ClassName} {item}, {o.DataConfig.DbRepoOperationContextName} ctx)";
            UseRepositoriesContext = false;            
            ItemName = "item";

            SelectTypes = (types) => types.Select(t => new DbRepoCoreGenItem(t)
            {
                Props = SelectProps(t).Where(x =>
                    x.IsCurrentLoggedInPerson)
                    .ToArray()
            })
            .Where(t => t.Props.Any());
        }

        public override void OProp()
        {
            var item = Current.Item;
            var type = Current.Type;
            var prop = Current.Prop;            

            if (prop.IsCurrentLoggedInPerson)
            {
                O("var userId = GetCurrentUserInfo().UserId;");
                O($"if ({item}.{prop.Name} == null)");
                O($"    {item}.{prop.Name} = userId;");
                O($"else if ({item}.{prop.Name} != userId)");
                O($"    ThrowStaticUserIdPropMustNotBeModified<{type.ClassName}>(\"{prop.Name}\");");
            }
        }       
    }
}