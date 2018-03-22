using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojFormedNavigationPath
    {
        public static readonly MojFormedNavigationPath None = new MojFormedNavigationPath();

        public MojFormedNavigationPath()
        { }

        public bool Is
        {
            get { return Steps != null && Steps.Count != 0; }
        }

        public bool IsVia(MojType type)
        {
            if (!Is) return false;
            return Steps.Any(x => x.SourceType == type || x.TargetType == type);
        }

        public int StepIndexOfTarget(MojType type)
        {
            if (!Is) return -1;

            int i = 0;
            foreach (var step in Steps)
            {
                if (step.TargetType == type)
                    return i;
                i++;
            }

            return -1;
        }

        [DataMember]
        public List<MojFormedNavigationPathStep> Steps { get; set; }

        [DataMember]
        public bool IsForeign { get; set; }

        [DataMember]
        public string TargetPath { get; set; }

        [DataMember]
        public string SourcePath { get; set; }

        public string TargetAliasPath
        {
            get
            {
                var path = StepPropNames(target: false).Join(".");
                if (TargetProp != null)
                    path += "." + TargetProp.Alias;

                return path;
            }
        }

        public MojType TargetType
        {
            get { return Last?.TargetType; }
        }

        public MojProp TargetProp
        {
            get { return Last?.TargetProp; }
        }

        public MojProp RootProp
        {
            get { return Root?.SourceProp; }
        }

        public MojFormedNavigationPathStep Root
        {
            get { return Steps?.FirstOrDefault(); }
        }

        public MojFormedNavigationPathStep Last
        {
            get { return Steps?.LastOrDefault(); }
        }

        public MojFormedNavigationPathStep FirstLooseStep
        {
            get
            {
                if (!Is) return null;

                foreach (var step in Steps)
                    if (step.SourceProp.Reference.Binding.HasFlag(MojReferenceBinding.Loose))
                        return step;

                return null;
            }
        }

        public IEnumerable<string> StepPropNames(bool target = true, MojType startType = null)
        {
            if (Steps == null || Steps.Count == 0)
                yield break;

            int start = startType == null ? 1 : 0;
            MojFormedNavigationPathStep step;
            for (int i = 0; i < Steps.Count; i++)
            {
                step = Steps[i];
                if (start == 0)
                {
                    if (step.SourceType == startType)
                        start = 1;
                    else if (step.TargetType == startType)
                        start = 2;
                }
                else if (start == 2)
                    start = 1;

                if (start == 1)
                    yield return step.SourceProp.Name;
            }                   

            if (target && start != 0)
            {
                var prop = TargetProp;
                if (prop != null)
                    yield return prop.Name;
            }
        }

        public string GetTargetPathFrom(MojType startType)
        {
            return StepPropNames(target: true, startType: startType).Join(".");
        }

        public MojProp ToTargetEntityProp()
        {
            var path = ToEntityPath();
            if (path == this)
                return path.TargetProp;

            var prop = path.TargetProp.Clone();
            prop.FormedNavigationFrom = path;

            return prop;
        }

        public MojProp ToRootEntityProp()
        {
            var path = ToEntityPath();

            var rootProp = path.RootProp;
            if (rootProp.FormedNavigationTo == path)
                return rootProp;

            // We are changing the prop's path, so operate on a clone of the prop.
            rootProp = rootProp.Clone();

            // If we are changing the path's root property, then ensure we operate on a clone of the path.
            if (path == this)
                path = Clone();

            path.Root.SourceProp = rootProp;
            rootProp.FormedNavigationTo = path;

            return rootProp;
        }

        public MojFormedNavigationPath Clone()
        {
            if (this == None) throw new MojenException("The NULL object must not be cloned.");

            var clone = new MojFormedNavigationPath();
            clone.IsForeign = IsForeign;
            clone.SourcePath = SourcePath;
            clone.TargetPath = TargetPath;
            clone.Steps = new List<MojFormedNavigationPathStep>();
            foreach (var step in Steps)
                clone.Steps.Add(step.Clone());

            return clone;
        }

        public bool? IsEntity()
        {
            return Steps?.All(x => x.IsEntity());
        }

        public MojFormedNavigationPath ToEntityPath()
        {
            if (!Is || IsEntity() == true)
                return this;

            // Return a clone.
            var path = Clone();
            foreach (var step in path.Steps)
            {
                if (step.SourceType != null)
                    step.SourceType = step.SourceType.RequiredStore;

                if (step.SourceProp != null)
                    step.SourceProp = step.SourceProp.RequiredStore;

                if (step.TargetType != null)
                    step.TargetType = step.TargetType.RequiredStore;

                if (step.TargetProp != null)
                    step.TargetProp = step.TargetProp.RequiredStore;
            }

            path.Build();

            return path;
        }

        public MojFormedNavigationPath BuildFor(MojFormedType type, MojProp targetProp)
        {
            var path = new MojFormedNavigationPath();
            path.Steps = Steps.Take(Steps.Count - 1).ToList();
            var last = Steps.Last().Clone();
            last.SourceFormedType = type;
            last.TargetProp = targetProp;
            path.Steps.Add(last);

            path.Build();

            return path;
        }

        public bool IsEqual(MojFormedNavigationPath other)
        {
            return TargetPath == other.TargetPath;
        }

        public MojFormedNavigationPathStep AddStep(MojFormedNavigationPathStep step)
        {
            Guard.ArgNotNull(step, nameof(step));
            Steps.Add(step);
            return step;
        }

        public MojFormedNavigationPath Build()
        {
            IsForeign = Steps != null && Steps.Count != 0;
            SourcePath = StepPropNames(target: false).Join(".");
            TargetPath = StepPropNames().Join(".");

            return this;
        }

        public override string ToString()
        {
            if (!Is) return "(None)";

            string result = "";

            int i = -1;
            foreach (var step in Steps)
            {
                i++;
                if (i == 0)
                    result += $"[{step.SourceType?.Name ?? "(Missing)"}].";
                else
                    result += $" -> ";

                if (step.SourceProp != null)
                    result += PropToString(step.SourceProp);
                else
                    result += "(None)";

                if (step.TargetProp != null)
                    result += $" -> " + PropToString(step.TargetProp);
            }

            return result;
        }

        string PropToString(MojProp prop)
        {
            return prop.Name + $"({prop.Reference.Binding})";
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojFormedNavigationPathStep : MojBase
    {
        [DataMember]
        public MojProp TargetProp { get; set; }

        [DataMember]
        public MojType TargetType { get; set; }

        public MojFormedType TargetFormedType { get; set; }

        [DataMember]
        public MojProp SourceProp { get; set; }

        [DataMember]
        public MojType SourceType { get; set; }

        public MojFormedType SourceFormedType { get; set; }

        public MojFormedNavigationPathStep Clone()
        {
            return (MojFormedNavigationPathStep)MemberwiseClone();
        }

        public override string ToString()
        {
            string result = "";
            if (SourceType != null)
                result += $"[{SourceType.Name}].{SourceProp.Name}";

            if (TargetType != null)
            {
                result += $" -> [{TargetType.Name}]";
                if (TargetProp != null)
                    result += $".{TargetProp.Name}";
            }

            return result;
        }
    }
}