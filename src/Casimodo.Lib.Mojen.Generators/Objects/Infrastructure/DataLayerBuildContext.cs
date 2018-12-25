using System;
using System.Collections.Generic;

namespace Casimodo.Lib.Mojen
{
    public class DataLayerBuildContext : MojenBuildContext
    {
        public MojType Tenant { get; set; }
        public MojType EnumEntity { get; set; }

        public void PropToTenant(MojModelBuilder builder, Action<MojModelPropBuilder> build = null)
        {
            if (Tenant != null)
            {
                var pbuilder = builder.Prop("Tenant").ToTenant(Tenant);
                build?.Invoke(pbuilder);
            }
        }

        public void PropToTenant(MojEntityBuilder builder, Action<MojEntityPropBuilder> build = null)
        {
            if (Tenant != null)
            {
                var pbuilder = builder.Prop("Tenant").ToTenant(Tenant);
                build?.Invoke(pbuilder);
            }
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

        public void PropTenantReference(MojModelBuilder builder, Action<MojModelPropBuilder> build = null)
        {
            Parent.PropToTenant(builder, build);
        }

        public MojModelPropBuilder PropDescription(MojModelBuilder builder)
        {
            // KABU TODO: Put length into settings.
            return builder.Prop("Description", 2048)
                .Id("077b11a2-e4da-4746-b131-1e4705ecaf11")
                .Multiline()
                .Display("Beschreibung");
        }

        public MojEntityPropBuilder PropDescription(MojEntityBuilder builder)
        {
            // KABU TODO: Put length into settings.
            return builder.Prop("Description", 2048)
                .Id("077b11a2-e4da-4746-b131-1e4705ecaf11")
                .Multiline()
                .Display("Beschreibung");
        }

        public MojModelPropBuilder PropComments(MojModelBuilder builder)
        {
            // KABU TODO: Put length into settings.
            return builder.Prop("Comments", 2048)
                .Id("8eee2f85-65ce-4f04-ba38-499149a6057b")
                .Multiline()
                .Display("Anmerkungen");
        }

        public MojEntityPropBuilder PropNotes(MojEntityBuilder builder)
        {
            // KABU TODO: Put length into settings.
            return builder.Prop("Notes", 2048)
                .Id("db10103b-c987-434d-87ca-5ba4bc8912db")
                .Multiline()
                .Display("Notizen");
        }

        public MojModelPropBuilder PropNotes(MojModelBuilder builder)
        {
            // KABU TODO: Put length into settings.
            return builder.Prop("Notes", 2048)
                .Id("db10103b-c987-434d-87ca-5ba4bc8912db")
                .Multiline()
                .Display("Notizen");
        }

        public void PropTenantReference(MojEntityBuilder builder, Action<MojEntityPropBuilder> build = null)
        {
            Parent.PropToTenant(builder, build);
        }

        List<Action> _referenceResolutionActions = new List<Action>();

        public void ResolveReferencesLater(Action action)
        {
            _referenceResolutionActions.Add(action);
        }

        public void ExecuteRegisterReferenceActions()
        {
            foreach (var action in _referenceResolutionActions)
                action();
        }
    }
}