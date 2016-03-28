using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Casimodo.Lib
{
    public static class ExpressionHelper
    {
        public static Expression<Func<T, string>> GetGroupKey<T>(string property)
        {
            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Property(parameter, property);
            return Expression.Lambda<Func<T, string>>(body, parameter);
        }

        public static Expression<Func<TObj, bool>> GetEqualityPredicate<TObj, TVal>(PropertyInfo prop, TVal value)
        {
            var param = Expression.Parameter(typeof(TObj), "x");

            var predicate =
                Expression.Lambda<Func<TObj, bool>>(
                    Expression.Equal(
                        Expression.Property(param, prop),

                        typeof(TVal) == prop.PropertyType ?
                            (Expression)Expression.Constant(value) :
                            (Expression)Expression.Convert(
                                Expression.Constant(value), prop.PropertyType)),
                        param);

            return predicate;
        }

    }
}