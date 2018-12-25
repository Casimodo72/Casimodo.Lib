using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public sealed class MojEntityPropBuilder : MojClassPropBuilder<MojEntityBuilder, MojEntityPropBuilder>
    {
        internal static MojEntityPropBuilder CreateDummyBuilder()
        {
            return new MojEntityPropBuilder();
        }

        protected override MojEntityPropBuilder StoreCore(Action<MojEntityPropBuilder> build)
        {
            var builder = This();

            if (build != null)
                build(builder);

            return builder;
        }

        public MojModelPropBuilder Model()
        {
            var modelBuilder = ParentPropBuilder as MojModelPropBuilder;
            if (modelBuilder == null)
                throw new InvalidOperationException("This store property builder has no parent model property builder assigned.");

            return modelBuilder;
        }

        /// <summary>
        /// Indicates that this entity property won't be added to the DB.
        /// </summary>
        public MojEntityPropBuilder NoDb()
        {
            PropConfig.IsExcludedFromDb = true;

            return this;
        }

        /// <summary>
        /// Force inclusion in OData model even if this properties in not part of the DB model.
        /// </summary>
        public MojEntityPropBuilder ForceOData()
        {
            PropConfig.IsExplicitelyIncludedInOData = true;

            return this;
        }

        /// <summary>
        /// Creates an index in the DB.
        /// </summary>
        public MojEntityPropBuilder Index()
        {
            // KABU TODO: We do not support non-unique indexes across multiple props yet.
            if (PropConfig.DbAnno.Index.Is)
                return This();

            if (PropConfig.DbAnno.Unique.Is)
                throw new MojenException("Syntax error: 'Index' must come before the 'Unique' definition.");

            if (!PropConfig.DbAnno.Is)
                PropConfig.DbAnno = new MojDbPropAnnotation(PropConfig);

            var index = new MojIndexConfig { Is = true }; ;

            PropConfig.DbAnno.Index = index;
            index.Participants.Add(new MojIndexParticipantConfig
            {
                Kind = MojIndexPropKind.IndexMember,
                Prop = PropConfig
            });

            TypeBuilder.TypeConfig.Indexes.Add(index);

            return This();
        }

        public MojEntityPropBuilder Error(string error)
        {
            var target = LastErrorHolder as IMojErrorMessageHolder;
            if (target != null)
                target.ErrorMessage = error;
            else
                throw new MojenException("There is no previous definition this error can be assigned to.");

            LastErrorHolder = null;

            return This();
        }

        public object LastErrorHolder { get; set; }

        public MojEntityPropBuilder Unique(params object[] per)
        {
            if (per != null)
            {
                // No nulls.
                if (per.Any(x => x == null))
                    throw new MojenException($"The unique-per item values must not be null.");

                // Only MojType or property name.
                if (per.Any(x => x.GetType() != typeof(string) && x.GetType() != typeof(MojType)))
                    throw new MojenException($"The unique-per item values must be either MojTypes or property names.");
            }

            Index();
            var index = PropConfig.DbAnno.Index;
            index.IsUnique = true;
            // Clear because we are adding the context prop at the end of the list of participants.
            index.Participants.Clear();

            // KABU TODO: INDEX-PROP-NULLABLE: Currently disabled since in object "Party" we have
            //   two potential index scenarios where only one index is actually active.
            //if (!PropConfig.Rules.IsRequired)
            //    throw new MojenException("All unique index member properties must be required.");

            PropConfig.DbAnno.Unique = new MojUniqueConfig
            {
                Is = true
            };
            LastErrorHolder = PropConfig.DbAnno.Unique;

            if (per != null)
            {
                var uniqueMembers = new List<object>(per);

                // If tenant is defined: Ensure the tenant key is always the first member of the unique expression.
                var tenantType = App.CurrentBuildContext.Get<DataLayerConfig>().Tenant;
                if (tenantType != null)
                {
                    if (uniqueMembers.Contains(tenantType))
                        throw new MojenException("The tenant must not be specified explicitely.");

                    uniqueMembers.Insert(0, tenantType);
                }

                var type = PropConfig.DeclaringType;
                MojType perType;
                MojProp perProp;
                MojIndexPropKind kind;
                foreach (var obj in uniqueMembers)
                {
                    kind = MojIndexPropKind.IndexMember;

                    perType = obj as MojType;
                    if (perType != null)
                    {
                        // Tenant
                        if (tenantType != null && perType == tenantType)
                            kind = MojIndexPropKind.TenantIndexMember;

                        perProp = type.FindReferenceWithForeignKey(to: perType, required: true)
                            .ForeignKey
                            .RequiredStore;
                    }
                    else
                        perProp = type.GetProp((string)obj)
                            .RequiredStore;

                    var item = new MojUniqueParameterConfig
                    {
                        Kind = kind,
                        Prop = perProp
                    };

                    PropConfig.DbAnno.Unique._parameters.Add(item);

                    index.Participants.Add(new MojIndexParticipantConfig
                    {
                        Kind = kind,
                        Prop = perProp
                    });

                    if (perType != tenantType)
                        PropConfig.CascadeFromProps.Add(perProp);
                }

                index.Participants.Add(new MojIndexParticipantConfig
                {
                    Kind = MojIndexPropKind.IndexMember,
                    Prop = PropConfig
                });
            }

            return This();
        }

        public MojEntityPropBuilder SequenceStart(Action<MexSelectionBuilder> build)
        {
            Guard.ArgNotNull(build, nameof(build));

            if (!PropConfig.DbAnno.Unique.Is ||
                !PropConfig.DbAnno.Sequence.Is)
                throw new MojenException("Syntax error: 'SequenceStart' must have a preceeding 'Unique' and 'Sequence' definition.");

            var builder = new MexSelectionBuilder();
            build(builder);

            var selectorPath = MojFormedPathUtils.BuildPath(PropConfig.DeclaringType, builder.Selector);
            PropConfig.DbAnno.Sequence.StartSelector = selectorPath;
            PropConfig.DbAnno.Sequence.Start = null;
            // Cannot be DB sequence is the sequence start is dynamic.
            PropConfig.DbAnno.Sequence.IsDbSequence = false;
            PropConfig.DbAnno.Unique._parameters.Add(new MojUniqueParameterConfig
            {
                Kind = MojIndexPropKind.StartSelector,
                Prop = selectorPath.Root.SourceProp
            });

            return This();
        }

        public MojEntityPropBuilder SequenceEnd(Action<MexSelectionBuilder> build)
        {
            Guard.ArgNotNull(build, nameof(build));

            if (!PropConfig.DbAnno.Unique.Is ||
                !PropConfig.DbAnno.Sequence.Is)
                throw new MojenException("Syntax error: 'SequenceEnd' must have a preceeding 'Unique' and 'Sequence' definition.");

            if (PropConfig.DbAnno.Sequence.StartSelector == null)
                throw new MojenException("Syntax error: 'SequenceEnd' must have a preceeding 'SequenceStart' definition.");

            var builder = new MexSelectionBuilder();
            build(builder);

            var selectorPath = MojFormedPathUtils.BuildPath(PropConfig.DeclaringType, builder.Selector);
            PropConfig.DbAnno.Sequence.EndSelector = selectorPath;
            // Cannot be DB sequence is the sequence start is dynamic.
            PropConfig.DbAnno.Sequence.IsDbSequence = false;
            PropConfig.DbAnno.Unique._parameters.Add(new MojUniqueParameterConfig
            {
                Kind = MojIndexPropKind.StartSelector,
                Prop = selectorPath.Root.SourceProp
            });

            return This();
        }

        /// <summary>
        /// The value will be generated using a sequence generator.
        /// </summary>
        public MojEntityPropBuilder Sequence(string name = null, int start = 1, int increment = 1, int min = 1, int max = 2147483647)
        {
            if (min < 1) throw new ArgumentOutOfRangeException(nameof(min), "Min must not be less than one.");
            if (min >= max) throw new ArgumentOutOfRangeException(nameof(min), "Min must not be greater than or equal to max.");
            if (start < 1) throw new ArgumentOutOfRangeException(nameof(start), "Start value must not be less than one.");
            if (start < min) throw new ArgumentOutOfRangeException(nameof(start), "Start value must not be less than min.");
            if (start > max) throw new ArgumentOutOfRangeException(nameof(start), "Start value must not be greater than max.");

            if (!PropConfig.DbAnno.Unique.Is)
                throw new MojenException("Syntax error: 'Sequence' must have a preceeding 'Unique' definition.");

            PropConfig.DbAnno.Sequence = new MojSequenceConfig
            {
                Is = true,
                IsDbSequence = !PropConfig.DbAnno.Unique.HasParams,
                Name = (name ?? PropConfig.DeclaringType.Name + PropConfig.Name) + "Sequence",
                Start = start,
                Increment = increment,
                Min = min,
                Max = max
            };

            return This();
        }
    }
}