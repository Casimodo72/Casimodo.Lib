using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.Entity.Infrastructure.Annotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Casimodo.Lib.Data.Builder
{
    public static class DbModelExtensions
    {
        public static PrimitivePropertyConfiguration Index(this PrimitivePropertyConfiguration prop, params IndexAttribute[] indexes)
        {
            return prop.HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(indexes));
        }

        public static PrimitivePropertyConfiguration Index(this PrimitivePropertyConfiguration prop, string name, int order, bool unique)
        {
            return prop.HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute(name, order) { IsUnique = unique }));
        }
    }
}
