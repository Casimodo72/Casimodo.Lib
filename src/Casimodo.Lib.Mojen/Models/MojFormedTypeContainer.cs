using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Casimodo.Lib.Mojen
{
    public class MojFormedTypeContainer
    {
        internal readonly Dictionary<int, MojProp> _props = new Dictionary<int, MojProp>();

        public MojFormedTypeContainer(MojType type)
        {
            Type = type;
            var index = 0;
            foreach (var prop in type.GetProps())
            {
                _props.Add(index, prop);
                index++;
            }
        }

        public MojType Type { get; private set; }

        public MojProp Get(int index)
        {
            return this[index];
        }

        public MojProp this[int index]
        {
            get { return _props[index]; }
        }
    }   
}