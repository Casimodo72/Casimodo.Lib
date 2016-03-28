// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.ComponentModel
{
    public interface ICommandErrorHandler
    {
        void HandleError(Exception ex);
    }
}
