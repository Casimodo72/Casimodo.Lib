using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public static class DbContextExtensions
    {
        public static void ReferenceLoading(this DbContext db, bool enabled)
        {
            db.Configuration.ProxyCreationEnabled = enabled;
            db.Configuration.LazyLoadingEnabled = enabled;
        }        
    }
}