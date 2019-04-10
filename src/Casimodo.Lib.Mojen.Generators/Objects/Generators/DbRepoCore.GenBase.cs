using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DbRepoCoreGenContext
    {
        public string Item { get; set; }
        public MojType Type { get; set; }
        public MojProp Prop { get; set; }
        public DbRepoCoreGenSoftRefItem ReferenceItem { get; set; }
    }

    public class DbRepoCoreGenItem
    {
        public DbRepoCoreGenItem(MojType type)
        {
            Type = type;
        }

        public MojType Type { get; set; }
        public MojProp[] Props { get; set; } = new MojProp[0];

        public DbRepoCoreGenSoftRefItem[] SoftReferences { get; set; } = new DbRepoCoreGenSoftRefItem[0];
    }

    public class DbRepoCoreGenSoftRefItem
    {
        public MojType ChildType { get; set; }
        public MojSoftReference[] References { get; set; }
    }

    /// <summary>
    /// If a parent object is updated then also update its nested referenced objects.
    /// </summary>
    public abstract class DbRepoCoreGenBase : MojenGenerator
    {
        public DbRepoCoreGenBase()
        {
            Scope = "DataContext";
        }

        public DataLayerConfig DataConfig { get; set; }

        public DbRepoCoreGenContext Current { get; set; }

        public string Name { get; set; }

        public string ItemName { get; set; }

        public string OnAnyTypeMethodName { get; set; }

        public Func<DbRepoCoreGenBase, string> AnyTypeMethodCall { get; set; } =
            (o) => $"void {o.OnAnyTypeMethodName}(object item, DbContext db)";

        public Func<string> AnyTypeMethodFilter { get; set; } = () => null;

        public string OnTypeMethodName { get; set; }

        public Func<DbRepoCoreGenBase, MojType, string> TypeMethodCall { get; set; } =
            (o, type) => $"{o.OnTypeMethodName}(item as {type.ClassName}, db);";

        public Func<DbRepoCoreGenBase, MojType, string, string> TypeMethod { get; set; } =
            (o, type, item) => $"bool {o.OnTypeMethodName}({type.ClassName} {item}, DbContext db)";

        public Func<DbRepoCoreGenBase, MojType, string, string> TypeMethodExtensionCall { get; set; } = null;
        public Func<DbRepoCoreGenBase, MojType, string, string> TypeMethodExtension { get; set; } = null;

        public Func<DbRepoCoreGenBase, string> RepositoriesContextGetter { get; set; } =
            (o) => $"var context = new {o.DataConfig.DbRepoContainerName}(({o.DataConfig.DbContextName})db);";

        public bool UseRepositoriesContext { get; set; } = true;

        public Func<IEnumerable<MojType>, IEnumerable<DbRepoCoreGenItem>> SelectTypes { get; set; } = (types) => Enumerable.Empty<DbRepoCoreGenItem>();

        protected override void GenerateCore()
        {
            DataConfig = App.Get<DataLayerConfig>();

            if (string.IsNullOrEmpty(DataConfig.DbRepositoryDirPath)) return;
            if (string.IsNullOrEmpty(DataConfig.DbRepositoryCoreName)) return;

            PerformWrite(Path.Combine(DataConfig.DbRepositoryDirPath, $"{DataConfig.DbRepositoryCoreName}.{Name}.generated.cs"),
                () => OForAllTypes());
        }

        public IEnumerable<DbRepoCoreGenItem> GetItems()
        {
            return SelectTypes(App.GetRepositoryableEntities());
        }

        protected IEnumerable<MojProp> SelectProps(MojType type)
        {
            return type.GetProps();
        }

        public virtual void OProp()
        {
            // NOP
        }

        public virtual void OSoftReference()
        {
            // NOP
        }

        public void OClassStart()
        {
            var ns = new List<string>(new[] {"System",
                "System.Collections.Generic",
                "System.Linq",
                "Casimodo.Lib",
                "Casimodo.Lib.Data"});

            ns.Add("Microsoft.EntityFrameworkCore");

            OUsing(ns.ToArray());

            ONamespace(DataConfig.DataNamespace);

            O($"public partial class {DataConfig.DbRepositoryCoreName}");
            Begin();
        }

        public void OClassEnd()
        {
            End();
            End();
        }

        public virtual void OHelpers()
        {
            // NOP
        }

        public virtual void OForAllTypes()
        {
            OClassStart();

            var items = GetItems().ToArray();

            O(AnyTypeMethodCall(this));
            Begin();
            O(AnyTypeMethodFilter());
            foreach (var typeItem in items)
                O(TypeMethodCall(this, typeItem.Type));
            End();
            O();

            Current = new DbRepoCoreGenContext();

            foreach (var typeItem in items)
            {
                var type = typeItem.Type;
                string item = ItemName ?? "parent"; // type.VName;

                O(TypeMethod(this, type, item));
                Begin();

                O($"if ({item} == null) return false;");

                if (UseRepositoriesContext && RepositoriesContextGetter != null)
                    O(RepositoriesContextGetter(this));

                // Properties
                O();
                foreach (var prop in typeItem.Props)
                {
                    Current.Item = item;
                    Current.Type = type;
                    Current.Prop = prop;

                    OProp();
                }

                // SoftReferences
                foreach (var referenceItem in typeItem.SoftReferences)
                {
                    Current.Item = item;
                    Current.Type = type;
                    Current.ReferenceItem = referenceItem;

                    OSoftReference();
                }

                if (TypeMethodExtensionCall != null)
                {
                    O(TypeMethodExtensionCall(this, type, item));
                }

                O("return true;");
                End();
                O();

                if (TypeMethodExtension != null)
                {
                    O(TypeMethodExtension(this, type, item));
                    O();
                }
            }

            OHelpers();

            OClassEnd();
        }

        public void OThrowRepoException(Action content)
        {
            Oo($"throw new DbRepositoryException(");
            content();
            oO(");");
        }
    }
}