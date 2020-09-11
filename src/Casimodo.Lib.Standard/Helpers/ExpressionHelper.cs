using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

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

        // Source: http://stackoverflow.com/questions/4417430/c-sharp-negate-an-expression
        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> one)
        {
            var candidateExpr = one.Parameters[0];
            var body = Expression.Not(one.Body);

            return Expression.Lambda<Func<T, bool>>(body, candidateExpr);
        }

        public static Expression<Func<T, bool>> GetContainsPredicate<T, TValue>(this IEnumerable<TValue> items, PropertyInfo property)
        {
            var instance = Expression.Constant(items);

            var method = typeof(Enumerable)
                    .GetMethods()
                    .Where(x => x.Name == "Contains")
                    .Single(x => x.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(TValue));

            var param = Expression.Parameter(typeof(T), "x");
            var value = Expression.Property(param, property);

            var call = Expression.Call(method, instance, value);

            // x => keys.Contains(x.Prop)
            return Expression.Lambda<Func<T, bool>>(call, param);
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