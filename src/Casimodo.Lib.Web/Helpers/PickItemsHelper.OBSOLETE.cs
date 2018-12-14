// KABU TODO: REMOVE? Not used anymore. Keep for a while.
#if (false)

using Dynamitey;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Web.Mvc;
using Casimodo.Lib;

namespace Casimodo.Lib.Web
{
    using System.Text;
    using PickItem = SimplePickItem;

    public class SimplePickItem
    {
        public object Value { get; set; }

        public string Text { get; set; }
    }

    public static class PickItemsHelper
    {
        const string EmptyDisplayValue = "[...]";

        static readonly ConcurrentDictionary<Tuple<Type, bool, bool>, List<SimplePickItem>> _enums = new ConcurrentDictionary<Tuple<Type, bool, bool>, List<SimplePickItem>>();

        public static MvcHtmlString ToJsArray<TItem>(IQueryable<TItem> query, string valueProp, string displayProp, bool nullable = false, bool id = true)
        {
            var sb = new StringBuilder();
            sb.o("[");

            int i = 0;
            if (query != null)
            {

                var selectExpression = id ? $"new({valueProp}, {displayProp})" : $"new({displayProp})";

                // KABU TODO: REMOVE: Not needed because we now handle this directly on the UI selectors.
#if (false)
                if (nullable)
                {
                    i++;
                    if (id)
                        sb.o($"{{value:'',text:'{EmptyDisplayValue}'}}");
                    else
                        sb.o($"{{text:'{EmptyDisplayValue}'}}");
                }
#endif

                var items = query.Select(selectExpression).ToDynamicArray();
                string value, text;
                foreach (var item in items)
                {
                    if (i++ > 0) sb.o(",");

                    text = Convert.ToString(Dynamic.InvokeGet(item, displayProp));

                    if (id)
                    {
                        value = Convert.ToString(Dynamic.InvokeGet(item, valueProp));
                        sb.o($"{{value:'{value}',text:'{text}'}}");
                    }
                    else
                        sb.o($"{{text:'{text}'}}");
                }
            }

            sb.o("]");

            return MvcHtmlString.Create(sb.ToString());
        }

        public static IEnumerable<object> ToSelectItems<TItem>(IQueryable<TItem> query, string dataValueField, string dataTextField, bool nullable = false, bool dummy = true)
            where TItem : class, new()
        {
            if (query == null)
                yield break;

            var selectExpression = string.Format("new({0}, {1})", dataValueField, dataTextField);

            // KABU TODO: REMOVE: Not needed because we now handle this directly on the UI selectors.
#if (false)
            if (nullable)
                yield return new PickItem { Text = EmptyDisplayValue };
#endif

            var items = query.Select(selectExpression).ToDynamicArray();
            foreach (var item in items)
            {
                yield return new PickItem
                {
                    Value = Convert.ToString(Dynamic.InvokeGet(item, dataValueField)),
                    Text = Convert.ToString(Dynamic.InvokeGet(item, dataTextField))
                };
            }
        }

        // KABU TODO: REMOVE?
#if (false)
        public static IEnumerable<PickItem> ToSelectItems<TItem>(this IEnumerable<TItem> items, string dataValueField, string dataTextField, bool nullable = false)
            where TItem : class
        {
            if (items == null)
                return new SelectList(Enumerable.Empty<TItem>());

            if (nullable)
            {
                var ivalProp = typeof(TItem).GetProperty(dataValueField);
                var itextProp = typeof(TItem).GetProperty(dataTextField);
                var list = new List<PickItem>();
                list.Add(new PickItem { Text = "[...]" });

                foreach (var item in items)
                {
                    list.Add(new PickItem
                    {
                        Value = ivalProp.GetValue(item).ToStringOrNull(),
                        Text = itextProp.GetValue(item) as string
                    });
                }

                return list;
            }

            return new SelectList(items, dataValueField, dataTextField);
        }
#endif

        //public static SelectList ToSelectList<TEnum>(this TEnum value, bool nullable = true, bool names = false)
        //{
        //    return ToSelectList(typeof(TEnum), nullable, names);
        //}

        public static SelectList ToSelectList<TEnum>(bool nullable = true, bool names = false)
        {
            return ToSelectList(typeof(TEnum), nullable, names);
        }

        /// <summary>
        /// Converts the given Enum type to SelectList.
        /// </summary>
        /// <param name="type">An Enum type.</param>
        static SelectList ToSelectList(Type type, bool nullable = true, bool names = false)
        {
            return ToSelectList(ToPickItems(type, nullable, names));
        }

        static SelectList ToSelectList(List<SimplePickItem> items)
        {
            return new SelectList(items, "Value", "Text");
        }

        public static MvcHtmlString ToJsArray<TEnum>(bool nullable = false, bool names = false)
        {
            var items = ToPickItems(typeof(TEnum), nullable, names);
            var sb = new StringBuilder();
            sb.o("[");
            int i = 0;
            foreach (var item in items)
            {
                if (i++ > 0) sb.o(",");
                sb.o($"{{value:'{item.Value}',text:'{item.Text}'}}");
            }
            sb.o("]");

            return MvcHtmlString.Create(sb.ToString());
        }

        static List<SimplePickItem> ToPickItems(Type type, bool nullable = true, bool names = false)
        {
            if (type == null) throw new ArgumentNullException("type");

            type = Nullable.GetUnderlyingType(type) ?? type;
            if (!type.IsEnum)
                throw new ArgumentException(string.Format("Type {0} is not an enum", type.Name));

            List<SimplePickItem> items;
            if (_enums.TryGetValue(Tuple.Create(type, nullable, names), out items))
                return items;

            items = new List<SimplePickItem>();
            // KABU TODO: REMOVE: Not needed because we now handle this directly on the UI selectors.
#if (false)
            if (nullable)
                items.Add(new SimplePickItem { Text = EmptyDisplayValue });
#endif

            foreach (var enumValue in Enum.GetValues(type))
            {
                var name = Enum.GetName(type, enumValue);
                var item = new SimplePickItem
                {
                    Value = names ? name : enumValue,
                    Text = name
                };
                items.Add(item);
                var display = CustomAttributeExtensions.GetCustomAttribute<DisplayAttribute>(type.GetField(name));
                if (display != null)
                    item.Text = display.GetName();
            }

            _enums.TryAdd(Tuple.Create(type, nullable, names), items);

            return items;
        }
    }
}
#endif