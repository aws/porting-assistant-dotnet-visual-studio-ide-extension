using System;
using System.Collections.Generic;
using System.Globalization;

using System.IO;
using Microsoft.VisualStudio.Settings;

namespace PortingAssistantVSExtensionClient.Common
{
    class SettingsStore
    {
        readonly WritableSettingsStore store;
        readonly string root;
        public SettingsStore(WritableSettingsStore store, string root)
        {
            ArgumentNotNull(store, nameof(store));
            ArgumentNotNull(root, nameof(root));
            ArgumentNotEmptyString(root, nameof(root));
            this.store = store;
            this.root = root;
        }

        public object Read(string property, object defaultValue)
        {
            return Read(null, property, defaultValue);
        }

        public void Write(string property, object value)
        {
            Write(null, property, value);
        }

        public object Read(string subpath, string property, object defaultValue)
        {
            ArgumentNotNull(property, nameof(property));
            ArgumentNotEmptyString(property, nameof(property));

            var collection = subpath != null ? Path.Combine(root, subpath) : root;
            store.CreateCollection(collection);

            if (defaultValue is bool)
                return store.GetBoolean(collection, property, (bool)defaultValue);
            else if (defaultValue is int)
                return store.GetInt32(collection, property, (int)defaultValue);
            else if (defaultValue is uint)
                return store.GetUInt32(collection, property, (uint)defaultValue);
            else if (defaultValue is long)
                return store.GetInt64(collection, property, (long)defaultValue);
            else if (defaultValue is ulong)
                return store.GetUInt64(collection, property, (ulong)defaultValue);
            return store.GetString(collection, property, defaultValue?.ToString() ?? "");
        }

        public void Write(string subpath, string property, object value)
        {
            ArgumentNotNull(property, nameof(property));
            ArgumentNotEmptyString(property, nameof(property));

            var collection = subpath != null ? Path.Combine(root, subpath) : root;
            store.CreateCollection(collection);

            if (value is bool)
                store.SetBoolean(collection, property, (bool)value);
            else if (value is int)
                store.SetInt32(collection, property, (int)value);
            else if (value is uint)
                store.SetUInt32(collection, property, (uint)value);
            else if (value is long)
                store.SetInt64(collection, property, (long)value);
            else if (value is ulong)
                store.SetUInt64(collection, property, (ulong)value);
            else
                store.SetString(collection, property, value?.ToString() ?? "");
        }



        private void ArgumentNotNull(object value, string name)
        {
            if (value != null) return;
            string message = String.Format(CultureInfo.InvariantCulture, "Failed Null Check on '{0}'", name);
            throw new ArgumentNullException(name, message);
        }

        private void ArgumentNotEmptyString(string value, string name)
        {
            if (value?.Length > 0) return;
            string message = String.Format(CultureInfo.InvariantCulture, "The value for '{0}' must not be empty", name);
            throw new ArgumentException(message, name);
        }

    }
}
