using System;

namespace Casimodo.Lib.Mojen
{
    public interface IMojModelPropBuilder
    {
        MojEntityPropBuilder Store();
    }

    // TODO: [DisplayFormat]
    public class MojModelPropBuilder : MojClassPropBuilder<MojModelBuilder, MojModelPropBuilder>,
        IMojModelPropBuilder
    {
        public MojModelPropBuilder NoSetter()
        {
            PropConfig.SetterOptions = MojPropGetSetOptions.None;

            return this;
        }

        public MojModelPropBuilder Setter(MojPropGetSetOptions options)
        {
            PropConfig.SetterOptions = options;

            return this;
        }

        public MojModelPropBuilder Scaffold(bool scaffold = true)
        {
            Attr(new MojAttr("ScaffoldColumn", 30).CArg("scaffold", scaffold));

            return this;
        }

        public MojEntityPropBuilder Store()
        {
            return StoreCore(null);
        }

        protected override MojEntityPropBuilder StoreCore(Action<MojEntityPropBuilder> build)
        {
            CheckRequiredStoreType();

            var prop = EnsureStoreProp();
            var builder = MojPropBuilder.Create<MojEntityPropBuilder>(TypeBuilder.GetEntityBuilder(), prop, this);

            build?.Invoke(builder);

            return builder;
        }

        void CheckRequiredStoreType()
        {
            var declaringType = TypeBuilder.TypeConfig;
            var baseType = declaringType.BaseClass;
            if (baseType != null && baseType.Store != null && declaringType.Store == null)
                throw new MojenException($"Cannot create a store for the property '{PropConfig.Name}' " +
                    $"because the containing type '{declaringType.ClassName}' has neither a local nor an inherited store type.");
        }
    }
}