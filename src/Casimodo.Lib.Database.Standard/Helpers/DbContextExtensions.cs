using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public static class DbContextExtensions
    {
        public static void UseLazyLoading(this DbContext db, bool enabled)
        {
            throw new NotSupportedException();
            //db.Configuration.ProxyCreationEnabled = enabled;
            //db.Configuration.LazyLoadingEnabled = enabled;
        }        
    }
}