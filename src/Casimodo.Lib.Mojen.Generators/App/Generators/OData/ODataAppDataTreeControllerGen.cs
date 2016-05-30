using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public class ODataAppDataTreeControllerGen : WebPartGenerator
    {
        public ODataAppDataTreeControllerGen()
        {
            Scope = "App";
        }

        WebODataBuildConfig OData { get; set; }

        protected override void GenerateCore()
        {
            OData = App.Get<WebODataBuildConfig>();

            PerformWrite(Path.Combine(OData.WebODataControllersDirPath, "AppDataTreeController.generated.cs"),
                () => GenerateController());
        }

        class TypeItem
        {
            public MojType Type { get; set; }
            public bool IsContainer { get; set; }
            public MojProp[] Props { get; set; }

            public DbRepoCoreGenSoftRefItem[] SoftReferences { get; set; }
        }

        void GenerateController()
        {
            var name = "AppDataTree";

            OUsing(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Threading",
                "System.Threading.Tasks",
                "System.Net",
                "System.Web.Http",
                "System.Web.Http.Controllers",
                "System.Web.OData",
                "System.Web.OData.Query",
                "System.Web.OData.Routing",
                "System.Data",
                "Casimodo.Lib",
                "Casimodo.Lib.Data",
                "Casimodo.Lib.Web",
                GetAllDataNamespaces()
            );

            ONamespace(OData.WebODataServicesNamespace);

            O($"public partial class {name}Controller : {OData.WebODataControllerBaseClass}");
            Begin();

            var types = App.GetConcreteTypes(MojTypeKind.Entity).ToArray();
            var typeItems = new List<TypeItem>();

            foreach (var type in types)
            {
                var item = new TypeItem
                {
                    Type = type,
                    Props = type.GetReferenceProps()
                        .Where(ObjectTreeHelper.IsReferenceToChild)
                        .OrderBy(x => x.Reference.Binding)
                        .ThenBy(x => x.Reference.Multiplicity)
                        .ToArray(),
                    SoftReferences = types
                        .Select(t => new DbRepoCoreGenSoftRefItem
                        {
                            ChildType = t,
                            References = t.SoftReferences.Where(r => ObjectTreeHelper.IsReferenceToParent(type, r)).ToArray()
                        })
                        .Where(x => x.References.Any())
                        .ToArray()
                };

                item.IsContainer = item.Props.Any() || item.SoftReferences.Any();

                typeItems.Add(item);
            }

            O($"void AddChildren(AppDataTreeNodeBuildContext ctx)");
            Begin();
            foreach (var type in types)
                O($"Add{type.Name}Children(ctx);");
            End();

            foreach (var typeItem in typeItems)
            {
                var type = typeItem.Type;
                var dataConfig = App.GetDataLayerConfig(type.DataContextName);

                O();
                O($"bool Add{type.Name}Children(AppDataTreeNodeBuildContext ctx)");
                Begin();
                O($"if (ctx.ParentTypeId != {dataConfig.GetTypeKeysClassName()}.{type.Name}Id) return false;");

                if (typeItem.IsContainer)
                {
                    O($"var parent = ctx.Repos.{type.PluralName}.Find(ctx.ParentId);");

                    O();

                    foreach (var prop in typeItem.Props)
                    {
                        var targetType = prop.Reference.ToType;
                        var targetTypeItem = typeItems.First(x => x.Type == targetType);
                        var targetRepo = targetType.PluralName;
                        var pick = targetType.FindPick();
                        var valueDisplayPropName = pick != null ? targetType.GetProp(pick.DisplayProp).Name : null;

                        O($"// {prop.Reference.Axis}, {prop.Reference.Binding}, {prop.Reference.Multiplicity}");

                        if (prop.Reference.IsToMany)
                        {
                            var propDisplay = !string.IsNullOrEmpty(prop.DisplayLabel)
                                ? prop.DisplayLabel
                                : !string.IsNullOrEmpty(targetType.DisplayName)
                                    ? targetType.DisplayName
                                    : prop.Name;

                            O($"foreach (var child in ctx.Repos.{targetRepo}.Query(includeDeleted: true).Where(x => x.{prop.Reference.ChildToParentProp.ForeignKey.Name} == parent.{type.Key.Name}))");
                            O($"    ctx.AddChild({BuildNode(targetTypeItem, "child", propDisplay, valueDisplayPropName)});");
                        }
                        else
                        {
                            var propDisplay = !string.IsNullOrEmpty(prop.ForeignKey.DisplayLabel)
                                ? prop.ForeignKey.DisplayLabel
                                : !string.IsNullOrEmpty(targetType.DisplayPluralName)
                                    ? targetType.DisplayPluralName
                                    : prop.Name;

                            O($"ctx.AddChild({BuildNode(targetTypeItem, $"ctx.Repos.{targetRepo}.Find(parent.{prop.Reference.ForeignKey.Name})", propDisplay, valueDisplayPropName)});");
                        }
                    }

                    // Soft child references                    
                    foreach (var item in typeItem.SoftReferences)
                    {
                        var targetType = item.ChildType;
                        var targetTypeItem = typeItems.First(x => x.Type == targetType);
                        var targetRepo = targetType.PluralName;

                        var pick = targetType.FindPick();
                        var valueDisplayPropName = pick != null ? targetType.GetProp(pick.DisplayProp).Name : null;

                        foreach (var reference in item.References)
                        {
                            if (reference.IsCollection)
                            {
                                O($"// Soft child collection of {targetType.ClassName}: {reference.Axis}, {reference.Binding}, {reference.Multiplicity}");

                                O($"foreach (var child in ctx.Repos.{targetRepo}.Query(includeDeleted: true).Where(x => {Mex.ToLinqPredicate(reference.Condition)}))");
                                O($"    ctx.AddChild({BuildNode(targetTypeItem, "child", reference.DisplayName, valueDisplayPropName)});");
                            }
                            else
                            {
                                O($"// Soft single child {targetType.ClassName}: {reference.Axis}, {reference.Binding}, {reference.Multiplicity}");

                                O($"ctx.AddChild({BuildNode(targetTypeItem, $"ctx.Repos.{targetRepo}.Query(includeDeleted: true).FirstOrDefault(x => {Mex.ToLinqPredicate(reference.Condition)})", reference.DisplayName, valueDisplayPropName)});");
                            }
                        }
                    }
                }

                O();
                O("return true;");

                End();
            }

            End(); // Class
            End(); // Namespace
        }

        string BuildNode(TypeItem itype, string expression, string propDisplay, string valueProp = null)
        {
            return $"CreateNode({expression}, {MojenUtils.ToCsValue(itype.IsContainer)}, {(propDisplay != null ? $"\"{propDisplay}\"" : "null")}{(valueProp != null ? $", \"{valueProp}\"" : "")})";
        }
    }
}