using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    public static class MojTypeExtensions
    {
        public static bool IsModel(this MojType type)
        {
            if (type == null) return false;
            return type.Kind == MojTypeKind.Model;
        }

        public static bool IsEntity(this MojType type, bool @default = false)
        {
            if (type == null) return @default;
            return type.Kind == MojTypeKind.Entity;
        }

        public static bool IsEntityOrModel(this MojType type)
        {
            if (type == null) return false;
            return type.Kind == MojTypeKind.Entity || type.Kind == MojTypeKind.Model;
        }

        public static bool IsEntityOrModel(this MojProp prop)
        {
            if (prop == null) return false;
            return prop.DeclaringType.IsEntity() || prop.DeclaringType.IsModel();
        }

        public static bool IsEntity(this MojProp prop, bool @default = false)
        {
            if (prop == null) return @default;
            return prop.DeclaringType.IsEntity();
        }

        public static bool IsModel(this MojProp prop)
        {
            if (prop == null) return false;
            return prop.DeclaringType.IsModel();
        }

        public static bool IsEntity(this MojFormedNavigationPathStep step)
        {
            if (!step.SourceType.IsEntity(true) ||
                !step.SourceProp.IsEntity(true) ||
                !step.TargetType.IsEntity(true) ||
                !step.TargetProp.IsEntity(true))
            {
                return false;
            }

            return true;
        }

        public static bool IsEnum(this MojType type)
        {
            if (type == null) return false;
            return type.Kind == MojTypeKind.Enum;
        }

        // TODO: REMOVE
        //public static bool IsEnumEntity(this MojType type)
        //{
        //    if (type == null) return false;
        //    return type.Kind == MojTypeKind.Entity && type.IsEnumEntity;
        //}

        public static bool IsComplex(this MojType type)
        {
            if (type == null) return false;
            return type.Kind == MojTypeKind.Complex;
        }
    }
}
