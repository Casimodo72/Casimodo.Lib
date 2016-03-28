using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    public static class MojDataGraphExtensions
    {
        public static IEnumerable<MojDataGraphNode> Merge(this IEnumerable<MojDataGraphNode> first, IEnumerable<MojDataGraphNode> second)
        {
            if (first == null)
                return second ?? Array.Empty<MojDataGraphNode>();

            if (second == null)
                return first ?? Array.Empty<MojDataGraphNode>();

            var merge = new List<MojDataGraphNode>();

            merge.AddRange(first);

            var references = merge.OfType<MojReferenceDataGraphNode>().ToArray();
            MojPropDataGraphNode secondProp;
            MojReferenceDataGraphNode firstReference, secondReference;
            foreach (var secondNode in second)
            {
                secondProp = secondNode as MojPropDataGraphNode;
                if (secondProp != null)
                {
                    if (!merge.Any(x => x.Equals(secondProp)))
                        merge.Add(secondProp);
                }
                else
                {
                    secondReference = (MojReferenceDataGraphNode)secondNode;

                    firstReference = references.FirstOrDefault(r =>
                        r.TargetType == secondReference.TargetType &&
                        r.SourceProp.Id == secondReference.SourceProp.Id);

                    if (firstReference == null)
                        merge.Add(secondReference);
                    else
                        firstReference.TargetItems = new List<MojDataGraphNode>(Merge(firstReference.TargetItems, secondReference.TargetItems));
                }
            }

            return merge;
        }

        public static MojDataGraphNode BuildDataGraph(
            this MojProp prop,
            bool includeKey = false,
            bool includeForeignKey = false,
            bool filterIsDeleted = false,
            MojReferenceBinding? binding = null)
        {
            return BuildDataGraph(Enumerable.Repeat(prop, 1),
                includeKey: includeKey,
                includeForeignKey: includeForeignKey,
                filterIsDeleted: filterIsDeleted,
                binding: binding)
                .Single();
        }

        public static IEnumerable<MojDataGraphNode> BuildDataGraph(
            this IEnumerable<MojProp> properties,
            bool includeKey = false,
            bool includeForeignKey = false,
            bool filterIsDeleted = false,
            MojReferenceBinding? binding = null)
        {
            // Top level leaf properties
            foreach (var prop in properties
                // Exclude navigation props, but include plain foreign-keys.
                .Where(x => !x.Reference.Is || (!x.Reference.IsNavigation && !x.FormedNavigationTo.Is))
                .DistinctBy(x => x.Name))
            {
                yield return new MojPropDataGraphNode { Prop = prop };
            }

            // Navigation properties

            var navigationPaths = properties
                .Where(prop => prop.FormedNavigationTo.Is)
                .DistinctBy(prop => prop.FormedTargetPath)
                .Select(x => x.FormedNavigationTo);

            var navigations = BuildDataGraph(navigationPaths,
                includeKey: includeKey,
                includeForeignKey: includeForeignKey,
                filterIsDeleted: filterIsDeleted,
                binding: binding);

            MojProp foreignKey;
            foreach (var node in navigations)
            {
                if ((foreignKey = TryGetForeignKey(node, includeForeignKey)) != null)
                    yield return new MojPropDataGraphNode
                    {
                        Prop = foreignKey
                    };

                yield return node;
            }
        }

        public static MojDataGraphNode BuildDataGraph(
            this MojFormedNavigationPath path,
            bool includeKey = false,
            bool includeForeignKey = false,
            bool filterIsDeleted = false,
            MojReferenceBinding? binding = null,
            int startDepth = 0)
        {
            return BuildDataGraph(Enumerable.Repeat(path, 1),
                includeKey: includeKey,
                includeForeignKey: includeForeignKey,
                filterIsDeleted: filterIsDeleted,
                binding: binding,
                startDepth: startDepth).Single();
        }

        public static List<MojDataGraphNode> BuildDataGraph(
            this IEnumerable<MojFormedNavigationPath> paths,
            bool includeKey = false,
            bool includeForeignKey = false,
            bool filterIsDeleted = false,
            MojReferenceBinding? binding = null,
            int startDepth = 0)
        {
            return BuildNavigationTreeCore(paths.Where(path => path.IsForeign), 0,
                includeKey: includeKey,
                includeForeignKey: includeForeignKey,
                filterIsDeleted: filterIsDeleted,
                binding: binding,
                startDepth: startDepth)
                .ToList();
        }

        static IEnumerable<MojDataGraphNode> BuildNavigationTreeCore(
            IEnumerable<MojFormedNavigationPath> paths,
            int depth,
            bool includeKey,
            bool includeForeignKey = false,
            bool filterIsDeleted = false,
            MojReferenceBinding? binding = null,
            int startDepth = 0)
        {
            var pathsAtDepth = paths.Where(path => path.Steps.Count > depth).ToArray();
            if (!pathsAtDepth.Any())
                yield break;

            MojFormedNavigationPathStep step;
            MojProp key;

            // Group by navigation property at current depth.
            foreach (var pathsByProp in pathsAtDepth.GroupBy(path => path.Steps[depth].SourceProp.Id))
            {
                step = pathsByProp.First().Steps[depth];
                var result = new MojReferenceDataGraphNode();
                result.SourceProp = step.SourceProp;
                result.TargetType = step.TargetType;

                key = null;
                if (includeKey)
                {
                    // Add the key property.
                    key = result.TargetType.Key;
                    result.TargetItems.Add(new MojPropDataGraphNode { Prop = key });
                }

                var deeperPaths = new List<MojFormedNavigationPath>();
                foreach (var path in pathsByProp)
                {
                    step = path.Steps[depth];
                    if (step == path.Steps.Last())
                    {
                        // This is a path leaf. Add the target property.

                        var targetProp = step.TargetProp;
                        if (targetProp != null && targetProp != key)
                            result.TargetItems.Add(new MojPropDataGraphNode { Prop = targetProp });
                    }
                    else
                    {
                        // This path is not finished yet.

                        // Filter by reference binding.
                        if (BindingMatches(step, binding))
                        {
                            deeperPaths.Add(path);
                        }
                    }
                }

                if (deeperPaths.Any())
                {
                    // Process deeper paths.
                    MojProp foreignKey;
                    foreach (var node in BuildNavigationTreeCore(deeperPaths, depth + 1, includeKey))
                    {
                        if ((foreignKey = TryGetForeignKey(node, includeForeignKey)) != null)
                            result.TargetItems.Add(new MojPropDataGraphNode
                            {
                                Prop = foreignKey
                            });

                        result.TargetItems.Add(node);
                    }

                }

                if (startDepth <= depth)
                    yield return result;
                else
                {
                    foreach (var child in result.TargetItems)
                        yield return child;
                }
            }
        }

        static MojProp TryGetForeignKey(MojDataGraphNode node, bool includeForeignKey)
        {
            MojReferenceDataGraphNode navigation = null;
            if (includeForeignKey &&
                (navigation = node as MojReferenceDataGraphNode) != null &&
                // One-To-Many navigation properties do not have foreign keys.
                !navigation.SourceProp.Reference.IsToMany)
            {
                return navigation.SourceProp.Reference.ForeignKey;
            }

            return null;
        }

        static bool BindingMatches(MojFormedNavigationPathStep step, MojReferenceBinding? binding)
        {
            if (binding == null) return true;

            return step.SourceProp.Reference.Binding == binding;
        }
    }

    public abstract class MojDataGraphNode
    {
        public abstract bool Equals(MojDataGraphNode other);

        public static bool Equals(IEnumerable<MojDataGraphNode> a, IEnumerable<MojDataGraphNode> b)
        {
            if ((a == null || !a.Any()) != (b == null || !b.Any()))
                return false;

            if (!a.Any())
                return true;

            if (a.Count() != b.Count())
                return false;

            foreach (var item in a)
                if (!b.Any(x => x.Equals(item)))
                    return false;

            return true;
        }
    }

    public sealed class MojPropDataGraphNode : MojDataGraphNode
    {
        public MojProp Prop { get; set; }

        public override string ToString()
        {
            return Prop.Name;
        }

        public override bool Equals(MojDataGraphNode node)
        {
            var other = node as MojPropDataGraphNode;
            if (other == null) return false;

            return Prop.Id == other.Prop.Id;
        }
    }

    public sealed class MojReferenceDataGraphNode : MojDataGraphNode
    {
        public MojReferenceDataGraphNode()
        {
            TargetItems = new List<MojDataGraphNode>();
        }

        public MojProp SourceProp { get; set; }

        public MojType TargetType { get; set; }

        public List<MojDataGraphNode> TargetItems { get; set; }

        public override bool Equals(MojDataGraphNode node)
        {
            var other = node as MojReferenceDataGraphNode;
            if (other == null) return false;
            if (other.TargetType != TargetType) return false;
            if (other.SourceProp.Id != SourceProp.Id) return false;

            var emptyItems = (TargetItems == null || TargetItems.Count == 0);
            if (emptyItems != (other.TargetItems == null || other.TargetItems.Count == 0))
                return false;

            if (emptyItems)
                return true;

            return MojDataGraphNode.Equals(TargetItems, other.TargetItems);
        }
    }
}