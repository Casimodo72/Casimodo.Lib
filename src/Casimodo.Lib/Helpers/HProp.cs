// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Casimodo.Lib
{
    /// <summary>
    /// Property helper
    /// </summary>
    public static class HProp
    {
        public static HPropDispayInfo Display(Type type, string prop)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (prop == null) throw new ArgumentNullException("prop");

            PropertyInfo iprop = type.GetProperty(prop);
            if (iprop == null)
                throw new Exception(string.Format("Property '{0}' not found in type '{1}'.", prop, type.Name));

            var result = new HPropDispayInfo { Name = iprop.Name, Text = iprop.Name };

            var display = iprop.GetCustomAttribute<DisplayAttribute>();
            if (display != null)
            {
                result.Text = display.GetName();
                result.Description = display.GetDescription();
            }

            return result;
        }

        public static HPropDispayInfo Display<TType, TProp>(Expression<Func<TType, TProp>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            PropertyInfo iprop = GetProperty<TType, TProp>(expression);
            if (iprop == null)
                throw new Exception("Property not found.");

            var result = new HPropDispayInfo { Name = iprop.Name, Text = iprop.Name };

            var display = iprop.GetCustomAttribute<DisplayAttribute>();
            if (display != null)
            {
                result.Text = display.GetName();
                result.Description = display.GetDescription();
            }

            return result;
        }

        public static string DisplayName<TType, TProp>(Expression<Func<TType, TProp>> expression)
        {
            return Display(expression).Text;
        }

        // TODO: Temporary workaround until we can use nameof in C# 6.
        public static string Name<TType, TProp>(Expression<Func<TType, TProp>> expression)
        {
            return Display(expression).Name;
        }

        static PropertyInfo GetProperty<TType, TProp>(Expression<Func<TType, TProp>> expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");

            LambdaExpression lambda = expression as LambdaExpression;
            MemberExpression memberExpr;
            if (lambda.Body is UnaryExpression)
            {
                UnaryExpression unaryExpr = lambda.Body as UnaryExpression;
                memberExpr = unaryExpr.Operand as MemberExpression;
            }
            else
                memberExpr = lambda.Body as MemberExpression;

            PropertyInfo iprop = memberExpr.Member as PropertyInfo;

            return iprop;
        }

        public static bool HasProp(object item, string name)
        {
            Guard.ArgNotNull(item, nameof(item));
            Guard.ArgNotNullOrWhitespace(name, nameof(name));

            return item.GetTypeProperty(name) != null;
        }

        public static void SetProp(object item, string name, object value)
        {
            Guard.ArgNotNull(item, nameof(item));
            Guard.ArgNotNullOrWhitespace(name, nameof(name));

            item.GetTypeProperty(name)?.SetValue(item, value);
        }

        public static bool SetChangedProp<TValue>(object item, string name, TValue value)
        {
            Guard.ArgNotNull(item, nameof(item));
            Guard.ArgNotNullOrWhitespace(name, nameof(name));

            var prop = item.GetTypeProperty(name);
            if (prop == null)
                return false;

            var oldValue = (TValue)prop.GetValue(item);

            if (EqualityComparer<TValue>.Default.Equals(oldValue, value))
                return false;

            prop.SetValue(item, value);

            return true;
        }

        public static void MapProp<T>(object source, object target, string name, T defaultValue = default(T))
        {
            Guard.ArgNotNull(source, nameof(source));
            Guard.ArgNotNull(target, nameof(target));

            SetProp(target, name, GetProp(source, name, defaultValue));
        }

        public static bool MapChangedProp<T>(object source, object target, string name, T defaultValue = default(T))
        {
            Guard.ArgNotNull(source, nameof(source));
            Guard.ArgNotNull(target, nameof(target));

            return SetChangedProp(target, name, GetProp(source, name, defaultValue));
        }

        public static T GetProp<T>(object item, string name, T defaultValue = default(T))
        {
            Guard.ArgNotNull(item, nameof(item));
            Guard.ArgNotNullOrWhitespace(name, nameof(name));

            var prop = item.GetTypeProperty(name);
            if (prop == null)
                return defaultValue;

            return (T)prop.GetValue(item);
        }
    }
}