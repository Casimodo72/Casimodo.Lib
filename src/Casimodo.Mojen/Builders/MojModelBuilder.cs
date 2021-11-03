using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public interface IMojModelBuilder : IMojClassBuilder
    {

    }

    public sealed class MojModelBuilder : MojClassBuilder<MojModelBuilder, MojModelPropBuilder>,
        IMojModelBuilder
    {
        MojProp _lastProp;

        public MojModelBuilder Content(Action<MojModelBuilder> build)
        {
            build(This());

            return this;
        }

        protected override void OnPropAdding(MojProp modelProp)
        {
            if (modelProp.IsAutoRelated)
                return;

            if (_lastProp != null)
                ProcessPendingStoreProp(_lastProp);

            _lastProp = modelProp;
        }

        public MojModelBuilder WrapStore()
        {
            TypeConfig.IsStoreWrapper = true;
            return this;
        }

        public MojModelBuilder Store()
        {
            return Store(TypeConfig.Name, TypeConfig.PluralName);
        }

        public MojModelBuilder Store(string entityName, string entityPluralName = null)
        {
            return StoreCore(entityName, entityPluralName, null);
        }

        public MojModelBuilder Store(Action<MojEntityBuilder> buildAction)
        {
            return StoreCore(TypeConfig.Name, TypeConfig.PluralName, buildAction);
        }

        public MojModelBuilder Store(string entityName, Action<MojEntityBuilder> buildAction = null)
        {
            return StoreCore(entityName, null, buildAction);
        }

        MojModelBuilder StoreCore(string entityName, string entityPluralName = null, Action<MojEntityBuilder> buildAction = null)
        {
            if (TypeConfig.Store == null)
            {
                var entity = MojType.CreateEntity(entityName);
                if (entityPluralName != null)
                    entity.InitPluralName(entityPluralName);
                entity.IsAbstract = TypeConfig.IsAbstract;
                entity.Namespace = App.Get<DataLayerConfig>().DataNamespace;

                TypeConfig.Store = entity;
                TypeConfig.IsStoreOwner = true;

                if (TypeConfig.BaseClass != null)
                {
                    TypeConfig.Store.BaseClass = TypeConfig.BaseClass.Store;
                    TypeConfig.Store.BaseClassName = TypeConfig.BaseClass.Store.ClassName;
                }
            }

            buildAction?.Invoke(GetEntityBuilder());

            return this;
        }

        public MojModelBuilder Store(MojType entity)
        {
            TypeConfig.Store = entity;
            TypeConfig.IsStoreOwner = false;
            return this;
        }
    }
}