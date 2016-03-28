// Copyright (c) 2009 Kasimier Buchcik

// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Linq.Expressions;

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