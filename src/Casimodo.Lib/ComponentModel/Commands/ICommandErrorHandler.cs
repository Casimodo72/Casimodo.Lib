// Copyright (c) 2009 Kasimier Buchcik
using System;

namespace Casimodo.Lib.ComponentModel
{
    public interface ICommandErrorHandler
    {
        void HandleError(Exception ex);
    }
}