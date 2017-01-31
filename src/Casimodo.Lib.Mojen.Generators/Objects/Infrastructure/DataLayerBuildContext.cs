using System;

namespace Casimodo.Lib.Mojen
{
    public class DataLayerBuildContext : MojenBuildContext
    {
        public MojType Tenant { get; set; }
        public MojType EnumEntity { get; set; }

        public MojModelPropBuilder PropTenantReference(MojModelBuilder builder, bool nullable = true)
        {
            return builder.Prop("Tenant").TenantReference(to: Tenant, nullable: nullable);
        }

        public MojEntityPropBuilder PropTenantReference(MojEntityBuilder builder, bool nullable = true)
        {
            return builder.Prop("Tenant").TenantReference(to: Tenant, nullable: nullable);
        }
    }

    public class MojenDataLayerPackageBuildContext : MojenGeneratorBase
    {
        public MojenDataLayerPackageBuildContext(DataLayerBuildContext parent)
        {
            Parent = parent;
        }

        public MojenApp App
        {
            get { return Parent.App; }
        }

        public DataLayerBuildContext Parent { get; set; }

        public MojType Tenant
        {
            get { return Parent.Tenant; }
            set { Parent.Tenant = value; }
        }

        public MojType EnumEntity
        {
            get { return Parent.EnumEntity; }
            set { Parent.EnumEntity = value; }
        }

        public MojModelBuilder AddModel(string name)
        {
            return Parent.AddModel(name);
        }

        public MojModelBuilder BuildModel(MojType type)
        {
            return Parent.BuildModel(type);
        }

        public MojEntityBuilder AddEnumEntity(string name, string displayName, string displayNamePlural)
        {
            var builder = AddEntity(name)
                .Base(EnumEntity)
                .EnumEntity()
                .Display(displayName, displayNamePlural)
                .Use<ODataConfigGen>();
            
            return builder;
        }

        public MojEntityBuilder AddEntity(string name)
        {
            return Parent.AddEntity(name);
        }

        public MojEntityBuilder BuildEntity(MojType type)
        {
            return Parent.BuildEntity(type);
        }

        public MojComplexTypeBuilder AddComplex(string name)
        {
            return Parent.AddComplex(name);
        }

        public MojEnumBuilder AddEnum(string name)
        {
            return Parent.AddEnum(name);
        }

        public MojInterfaceBuilder AddInterface(string name)
        {
            return Parent.AddInterface(name);
        }

        public MojAnyKeysBuilder AddKeys(string className, Type valueType)
        {
            return Parent.AddKeys(className, valueType);
        }

        public MojValueSetContainerBuilder AddItemsOfType(MojType type)
        {
            return Parent.AddItemsOfType(type);
        }

        public MojValueSetContainerBuilder SeedEnumEntity(MojType type)
        {
            var seed = Parent.AddItemsOfType(type);
            seed.UseIndex().Name("Name").Value("Id")
                .Use<PrimitiveKeysGen>()
                .Use<JsPrimitiveKeysGen>()
                .Seed("Name", "DisplayName", "Id");

            return seed;
        }

        public MojModelPropBuilder PropTenantReference(MojModelBuilder builder, bool nullable = true)
        {
            return Parent.PropTenantReference(builder, nullable);
        }

        public MojEntityPropBuilder PropTenantReference(MojEntityBuilder builder, bool nullable = true)
        {
            return Parent.PropTenantReference(builder, nullable);
        }
    }
}