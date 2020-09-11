// Copyright (c) 2010 Kasimier Buchcik
using System;
using System.Reflection;

namespace Casimodo.Lib.ComponentModel
{
    public static class BindingHelper
    {
        public static PropertyInfo GetPropertyInfo(object source, string path)
        {
            object curObject = source;
            Type curType = source.GetType();
            PropertyInfo curProp = null;
            string[] steps = path.Split(new char[] { '.' });
            int max = steps.Length - 1;
            int i = 0;
            for (i = 0; i < steps.Length; i++)
            {
                curProp = curType.GetProperty(steps[i]);

                if (curProp == null)
                {
                    // If the type info didn't provide information about
                    // the property, then try to get the property via
                    // the instance itself - if available.

                    if (curObject == null)
                        return null;

                    curProp = curObject.GetTypeProperty(steps[i]);
                    if (curProp == null)
                        return null;
                }

                if (!curProp.CanRead)
                {
                    // You better give your property a getter.
                    return null;
                }

                if (i == max)
                    return curProp;

                // Move context over to the next property.
                curType = curProp.PropertyType;
                if (curObject != null)
                    curObject = curProp.GetValue(curObject, null);
            }

            return null;
        }

        public class BindingSourceInfo
        {
            public object SourceValue { get; set; }

            public PropertyInfo SourceProperty { get; set; }

            public object SourceItem { get; set; }
        }

        public static BindingSourceInfo GetBindingStates(object source, string path)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (path == null)
                throw new ArgumentNullException("path");

            object curInstance = source;
            Type curType = source.GetType();
            PropertyInfo curProp = null;
            string[] steps = path.Split(new char[] { '.' });
            int max = steps.Length - 1;
            int i = 0;
            for (i = 0; i < steps.Length; i++)
            {
                curProp = curType.GetProperty(steps[i]);

                if (curProp == null)
                {
                    // If the type info didn't provide information about
                    // the property, then try to get the property via
                    // the instance itself - if available.

                    if (curInstance == null)
                        break;

                    curProp = curInstance.GetTypeProperty(steps[i]);
                    if (curProp == null)
                        break;
                }

                if (!curProp.CanRead)
                {
                    // You better give your property a getter.
                    break;
                }

                if (i == max)
                {
                    BindingSourceInfo result = new BindingSourceInfo();
                    result.SourceItem = curInstance;
                    result.SourceProperty = curProp;
                    result.SourceValue = curProp.GetValue(curInstance, null);
                    return result;
                }

                // Move context over to the next property.
                curType = curProp.PropertyType;
                curInstance = curProp.GetValue(curInstance, null);

                if (curInstance == null)
                {
                    // We need an instance, so stop.
                    break;
                }
            }

            return null;
        }
    }
}