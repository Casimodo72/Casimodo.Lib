using System;

namespace Casimodo.Lib.Data
{
    public interface IDbFileInfo
    {
        Guid Id { get; }

        string OriginalFileName { get; }

        string FileName { get; }
    }
}