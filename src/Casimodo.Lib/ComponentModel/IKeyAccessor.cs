using System;

namespace Casimodo.Lib
{
    public interface IGuidGenerateable
    {
        void GenerateGuid();
    }

    public interface IIdGetter
    {
        Guid Id { get; }
    }

    public interface IKeyAccessor
    {
        object GetKeyObject();
    }

    // KABU TODO: REVISIT: Does this need to be generic?
    public interface IKeyAccessor<TKey> : IKeyAccessor
        where TKey : IComparable<TKey>
    {
        TKey GetKey();
    }
}