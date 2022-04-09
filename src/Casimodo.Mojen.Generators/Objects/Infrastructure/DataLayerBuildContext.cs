namespace Casimodo.Mojen
{
    public class DataLayerBuildContext : MojenBuildContext
    {
        public MojType Tenant { get; set; }
        public MojType EnumModel { get; set; }
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

    public class MojenDataLayerPackage : MojenGeneratorBase
    {
        public MojenDataLayerPackage(DataLayerBuildContext parent)
        {
            Context = parent;
        }

        public MojenApp App
        {
            get { return Context.App; }
        }

        public DataLayerBuildContext Context { get; set; }

        public List<MojBase> Items { get; set; } = new List<MojBase>();

        T Add<T>(T builder) where T : MojTypeBuilder
        {          
            Items.Add(builder.TypeConfig);
            return builder;
        }

        public MojType Tenant
        {
            get { return Context.Tenant; }
            set { Context.Tenant = value; }
        }

        public MojType EnumModel
        {
            get { return Context.EnumModel; }
            set { Context.EnumModel = value; }
        }

        public MojType EnumEntity
        {
            get { return Context.EnumEntity; }
            set { Context.EnumEntity = value; }
        }

        public MojModelBuilder AddModel(string name, string pluralName = null)
        {
            return Add(Context.AddModel(name, pluralName));
        }

        public MojModelBuilder BuildModel(MojType type)
        {
            return Add(Context.BuildModel(type));
        }

        public MojModelBuilder AddEnumModelAndStore(string name, string displayName, string displayNamePlural)
        {
            var builder = AddModel(name)
                .Base(EnumModel)
                .Display(displayName, displayNamePlural)
                .Use<ODataConfigGen>()
                .Store();

            Add(builder);

            return builder;
        }

        public MojEntityBuilder AddEntity(string name)
        {
            return Add(Context.AddEntity(name));
        }

        public MojEntityBuilder BuildEntity(MojType type)
        {
            return Add(Context.BuildEntity(type));
        }

        public MojComplexTypeBuilder AddComplex(string name)
        {
            return Add(Context.AddComplex(name));
        }

        public MojEnumBuilder AddEnum(string name)
        {
            return Add(Context.AddEnum(name));
        }

        public MojInterfaceBuilder AddInterface(string name)
        {
            return Add(Context.AddInterface(name));
        }

        public MojAnyKeysBuilder AddKeys(string className, Type valueType)
        {
            return Context.AddKeys(className, valueType);
        }

        public MojValueSetContainerBuilder AddItemsOfType(MojType type)
        {
            return Context.AddItemsOfType(type);
        }

        public void PropTenantReference(MojModelBuilder builder, Action<MojModelPropBuilder> build = null)
        {
            Context.PropToTenant(builder, build);
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
            // TODO: Put length into settings.
            return builder.Prop("Notes", 2048)
                .Id("db10103b-c987-434d-87ca-5ba4bc8912db")
                .Multiline()
                .Display("Notizen");
        }

        public void PropTenantReference(MojEntityBuilder builder, Action<MojEntityPropBuilder> build = null)
        {
            Context.PropToTenant(builder, build);
        }

        readonly List<Action> _referenceResolutionActions = new();

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