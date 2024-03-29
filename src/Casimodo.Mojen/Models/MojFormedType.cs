﻿namespace Casimodo.Mojen
{
    public class MojFormedType : MojBase, IFormedTypePropAccessor
    {
        Dictionary<int, MojProp> _props = [];

        public MojFormedType()
        { }

        public MojFormedType(MojFormedNavigationPath from, MojType type)
        {
            _Type = type;
            FormedNavigationFrom = from;
        }

        public MojFormedNavigationPath FormedNavigationFrom { get; set; } = MojFormedNavigationPath.None;

        public MojType _Type { get; set; }

        internal void Via(MojType type, MojProp prop)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var path = EnsureNavigationPath();
            path.Steps.Add(new MojFormedNavigationPathStep { SourceType = type, SourceProp = prop, TargetFormedType = this, TargetType = _Type });
            path.Build();
        }

        internal void Via(MojFormedNavigationPath path)
        {
            if (path == null || path.Steps == null || !path.Steps.Any()) return;

            var mypath = EnsureNavigationPath();
            mypath.Steps.AddRange(path.Steps);
            mypath.Build();
        }

        MojFormedNavigationPath EnsureNavigationPath()
        {
            if (!FormedNavigationFrom.Is)
            {
                FormedNavigationFrom = new MojFormedNavigationPath
                {
                    Steps = []
                };
            }

            return FormedNavigationFrom;
        }

        public void Add(MojFormedTypeContainer container)
        {
            _Type = container.Type;
            _props = new Dictionary<int, MojProp>(container._props);
        }

        public MojProp GetPickDisplayProp(bool required = true)
        {
            var pick = _Type.FindPick();
            if (pick == null)
            {
                if (required)
                    throw new MojenException($"The type '{_Type.ClassName}' does no pick-display property defined.");

                return null;
            }

            return Get(pick.DisplayProp);
        }

        public MojProp Get(int index)
        {
            return this[index];
        }

        public MojProp Get(string name, bool required = true)
        {
            if (!required && !_props.Values.Any(x => x.Name == name))
                return null;

            return this[_props.First(x => x.Value.Name == name).Key];
        }

        public MojProp this[int index]
        {
            get
            {
                var prop = _props[index];
                if (FormedNavigationFrom.Is && !prop.FormedNavigationFrom.Is)
                {
                    prop = prop.Clone();
                    prop.FormedNavigationFrom = FormedNavigationFrom.BuildFor(this, prop);
                    _props[index] = prop;
                }

                return prop;
            }
        }

        public override string ToString()
        {
            return _Type != null ? _Type.ToString() : "[null]";
        }
    }
}