using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public interface IDbFileInfo
    {
        Guid Id { get; }

        string KindName { get; }

        string OriginalFileName { get; }

        string FileName { get; }
    }
}