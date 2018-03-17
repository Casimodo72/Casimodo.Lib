﻿using Casimodo.Lib.Mojen.Meta;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class MojUsedByEventArgs : EventArgs
    {
        public Type UsedType { get; set; }
        public object UsedByObject { get; set; }
    }

    public delegate void MojUsedByEventHandler(object source, MojUsedByEventArgs args);

    public class MojenApp
    {
        public MojenApp()
        {
            Items = new List<MojBase>();
            Generators = new List<MojenGenerator>();
            Contexts = new List<MojenBuildContext>();
            Configs = new List<MojenBuildConfig>();
        }
        public void LoadMeta(MojenMetaContainer container)
        {
            var items = container.GetItems<AppBuildConfig>().ToArray();
            Items.AddRange(items);
        }

        public static void HandleUsingBy(MojUsedByEventArgs args)
        {
            UsingBy?.Invoke(null, args);
        }

        public static event MojUsedByEventHandler UsingBy;

        public Action<MojenApp> Prepare { get; set; }

        public void Execute()
        {
            if (Prepare != null)
                Prepare(this);

            CurrentBuildContext = null;
            foreach (var context in Contexts)
            {
                CurrentBuildContext = context;
                ExecuteGenerators("Context");

                foreach (var ctx in context.GetItems<DataLayerConfig>())
                {
                    CurrentScopeObject = ctx;
                    context.CurrentDataContext = ctx;
                    ExecuteGenerators("DataContext");
                }
                CurrentScopeObject = null;
                context.CurrentDataContext = null;

                foreach (var ctx in GetItems<DataViewModelLayerConfig>())
                {
                    CurrentScopeObject = ctx;
                    context.CurrentModelContext = ctx;
                    ExecuteGenerators("ModelContext");
                }
                context.CurrentModelContext = null;
                CurrentScopeObject = null;

            }
            CurrentBuildContext = null;

            ExecuteGenerators("App");

            foreach (var meta in GetItems<MojenAppMetadataConfig>())
            {
                CurrentScopeObject = meta;
                ExecuteGenerators(scope: "Meta");
                CurrentScopeObject = null;
            }
        }

        void ExecuteGenerators(string scope)
        {
            foreach (var generatorPrototype in Generators.Where(x => x.Scope == scope))
            {
                var generator = (MojenGenerator)Activator.CreateInstance(generatorPrototype.GetType());
                generator.Initialize(this);
                generator.Generate();
            }
        }

        public DateTimeOffset Now { get; set; }

        public List<MojBase> Items { get; private set; }

        public List<MojenBuildConfig> Configs { get; private set; }

        public List<MojenGenerator> Generators { get; set; }

        public List<MojenBuildContext> Contexts { get; private set; }

        public MojenBuildContext CurrentBuildContext { get; set; }

        public object CurrentScopeObject { get; set; }

        public IEnumerable<MojType> AllModels
        {
            get { return GetTopTypes(MojTypeKind.Model); }
        }

        public IEnumerable<MojType> AllEntities
        {
            get { return GetTypes(MojTypeKind.Entity); }
        }

        public IEnumerable<MojType> AllConcreteEntities
        {
            get { return GetTypes(MojTypeKind.Entity).Where(x => !x.IsAbstract); }
        }

        public IEnumerable<MojValueSetContainer> AllValueCollections
        {
            get { return GetItems<MojValueSetContainer>(); }
        }

        //public string GetAppSetting(string name)
        //{
        //    string result;
        //    foreach (var config in Configs)
        //        if (config.Items.TryGetValue(name, out result))
        //            return result;

        //    throw new MojenException(string.Format("App configuration item not found ('{0}').", name));
        //}

        public void Add(MojenBuildContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            context.Initialize(this);
            Contexts.Add(context);
        }

        public void Add(MojenBuildConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            Configs.Add(config);
        }

        public T Config<T>()
            where T : class
        {
            var result = Configs.OfType<T>().FirstOrDefault();
            if (result != null)
                return result;

            return Get<T>();
        }

        public void Add(MojenGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException("generator");
            generator.Initialize(this);
            Generators.Add(generator);
        }



        public void Add(MojBase item)
        {
            if (item == null) throw new ArgumentNullException("item");
            Items.Add(item);
        }

        public IEnumerable<MojType> GetTopTypes(params MojTypeKind[] kinds)
        {
            return GetItems<MojType>()
                .Where(x => kinds == null || kinds.Length == 0 || kinds.Contains(x.Kind))
                .OrderBy(x => x.Name)
                .DistinctBy(x => x)
                .ToArray();
        }

        public IEnumerable<MojType> GetConcreteTypes(params MojTypeKind[] kinds)
        {
            return GetTypes(kinds).Where(x => !x.IsAbstract);
        }

        public IEnumerable<MojType> GetTypes(params MojTypeKind[] kinds)
        {
            var items = GetTopTypes(kinds);

            // Add non top-level entities.
            if (kinds == null || kinds.Length == 0 || kinds.Contains(MojTypeKind.Entity))
            {
                items = items.Concat(GetTopTypes(MojTypeKind.Model)
                    .Where(x => x.Store != null && x.IsStoreOwner)
                    .Select(x => x.Store))
                    .OrderBy(x => x.Name);
            }

            items = items.DistinctBy(x => x);

            return items;
        }

        public IEnumerable<MojType> GetTypesExcept(params MojTypeKind[] kinds)
        {
            if (kinds == null || kinds.Length == 0)
                return GetTypes();

            return GetTypes().Where(x => !kinds.Contains(x.Kind));
        }

        public IEnumerable<MojType> GetRepositoryableTypes()
        {
            return GetTypes(MojTypeKind.Model).Where(x => !x.IsAbstract && x.IsKeyAccessibleEffective())
                .Concat(GetRepositoryableEntities())
                .DistinctBy(x => x)
                .OrderBy(x => x.Name);
        }

        public IEnumerable<MojType> GetRepositoryableEntities()
        {
            return AllConcreteEntities.Where(x => x.IsKeyAccessibleEffective());
        }

        public T Get<T>(bool required = true)
            where T : class
        {
            if (CurrentScopeObject != null && CurrentScopeObject.GetType() == typeof(T))
                return (T)CurrentScopeObject;


            var item = GetItems<T>().FirstOrDefault();
            if (item != null)
                return item;

            if (required)
                throw new MojenException($"The type '{nameof(T)}' was not found in the Mojen App.");

            return null;
        }

        public DataLayerConfig GetDataLayerConfig(string name)
        {
            return GetItems<DataLayerConfig>().Where(x => x.Name == name).First();
        }

        public IEnumerable<MojBase> GetItems(Type type)
        {
            var result = Items.Where(x => x.GetType() == type);

            result = result.Concat(Configs.Where(x => x.GetType() == type));

            if (CurrentBuildContext != null)
            {
                result = result.Concat(CurrentBuildContext.Items.Where(x => x.GetType() == type));
            }
            else
            {
                result = result.Concat(Contexts.SelectMany(ctx => ctx.Items.Where(x => x.GetType() == type)));
            }

            return result.Distinct().ToArray();
        }

        public IEnumerable<T> GetItems<T>()
        {
            if (CurrentBuildContext != null)
                return Items.OfType<T>().Concat(CurrentBuildContext.Items.OfType<T>()).Distinct().ToArray();

            return Items.OfType<T>().Concat(Contexts.SelectMany(x => x.Items.OfType<T>())).Distinct().ToArray();
        }

        public IEnumerable<string> GetForeignDataNamespaces(string ns)
        {
            foreach (var dataContext in GetItems<DataLayerConfig>())
                if (dataContext.DataNamespace != ns)
                    yield return dataContext.DataNamespace;
        }
    }
}