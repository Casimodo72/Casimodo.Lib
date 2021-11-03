using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Casimodo.Lib.Data
{
    public interface IMoDataSnapshot
    {
        MoDataSnapshot GetSnapshot();

        void SetSnapshot(MoDataSnapshot snapshot);
    }

    public class MoDataSnapshot
    {
        public MoDataSnapshot()
        {
            Properties = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Properties { get; private set; }

        public static bool Changed(object item)
        {
            if (item == null)
                return true;

            var snapshotObj = item as IMoDataSnapshot;
            if (snapshotObj == null)
                return true;

            var snapshot = snapshotObj.GetSnapshot();
            if (snapshot == null)
                return true;

            Type type = item.GetType();

            // Evaluate whether there are any tracked properties.
            List<string> properties = MoDataSnapshot.Get(type);
            if (properties == null)
                return true;

            // Check tracked properties for changes.
            object originalValue;
            object currentValue;
            foreach (var prop in properties)
            {
                if (snapshot.Properties.TryGetValue(prop, out originalValue))
                {
                    // Get the current property value.
                    currentValue = type.GetProperty(prop).GetValue(item, null);

                    // Compare the property values.
                    // TODO: IMPROVE comparison
                    if (!object.Equals(currentValue, originalValue))
                    {
                        // Return true if at least one property value did change.
                        return true;
                    }
                }
            }

            return false;
        }

        public bool Snapshot(object item)
        {
            if (item == null)
                return false;

            var snapshotObj = item as IMoDataSnapshot;
            if (snapshotObj == null)
                return false;

            Type type = item.GetType();

            // Evaluate whether there are any tracked properties.
            List<string> properties = MoDataSnapshot.Get(type);
            if (properties == null)
                throw new InvalidOperationException("Cannot make snapshot: No properties registered for change tracking.");

            var snapshot = new MoDataSnapshot();
            foreach (var prop in properties)
            {
                snapshot.Properties.Add(prop, type.GetProperty(prop).GetValue(this, null));
            }

            snapshotObj.SetSnapshot(snapshot);

            return true;
        }

        static readonly Dictionary<Type, List<string>> _items = new Dictionary<Type, List<string>>();

        public static void Add(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            string[] propertyNames =
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.GetCustomAttributes(typeof(TrackChangesAttribute), true)
                    .Any())
                    .Select(x => x.Name)
                    .ToArray();

            Add(type, propertyNames);
        }

        public static void Add(Type type, params string[] propertyNames)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            List<string> properties;
            if (!_items.TryGetValue(type, out properties))
            {
                properties = new List<string>();
                _items.Add(type, properties);
            }

            foreach (var prop in propertyNames)
                properties.Add(prop);
        }

        public static List<string> Get(Type type)
        {
            List<string> properties;
            if (_items.TryGetValue(type, out properties))
                return properties;

            return null;
        }
    }
}