using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Casimodo.Lib.ComponentModel
{
    // KABU: TODO: Use a dedicated foreign open-source lib or perform refactoring.
    public static class PropertyHelper
    {
        public static string PropertyName<TType, TProp>(this TType type, Expression<Func<TType, TProp>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            return GetName<TType, TProp>(expression);
        }

        public static string GetName<TType, TProp>(Expression<Func<TType, TProp>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            PropertyInfo prop = GetInfo<TType, TProp>(expression);
            if (prop == null)
                return null;

            return prop.Name;
        }

        public static PropertyInfo GetInfo<TType, TProp>(Expression<Func<TType, TProp>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            LambdaExpression lambda = expression as LambdaExpression;
            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                UnaryExpression unaryExpression = lambda.Body as UnaryExpression;
                memberExpression = unaryExpression.Operand as MemberExpression;
            }
            else
            {
                memberExpression = lambda.Body as MemberExpression;
            }
            PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;

            return propertyInfo;
        }
    }
}