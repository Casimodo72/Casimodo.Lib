using System;

namespace Casimodo.Lib.ComponentModel
{
    public interface ICommandErrorHandler
    {
        void HandleError(Exception ex);
    }
}