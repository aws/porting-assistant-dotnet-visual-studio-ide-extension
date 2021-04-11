﻿using PortingAssistantVSExtensionClient.Common;
using System;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using PortingAssistantVSExtensionClient.Models;
using System.Globalization;
using System.IO;
using PortingAssistantVSExtensionClient.Utils;

namespace PortingAssistantVSExtensionClient.Options
{
    public sealed class UserSettings
    {
        private static readonly UserSettings instance = new UserSettings();

        private bool _isLoaded;
        public bool ShowWelcomePage;
        public bool EnabledMetric;
        public bool EnabledContinuousAssessment;
        public bool ApplyPortAction;
        public string CustomerEmail;
        public string CacheFolder;
        public string TargetFramework;

        readonly WritableSettingsStore _settingStore;

        private UserSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var sm = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            _settingStore = sm.GetWritableSettingsStore(SettingsScope.UserSettings);
        }


        public static UserSettings Instance
        {
            get
            {
                if (!instance._isLoaded)
                {
                    instance.LoadingAllSettings();
                    instance._isLoaded = true;
                }
                return instance;
            }
        }

        public T ReadValue<T>(string property, string value)
        {
            return (T)Read(property, value);
        }

        public void WriteValue(string property, object value)
        {
            Write(property, value);
        }

        public void SaveAllSettings()
        {
            Write("ShowWelcomePage", ShowWelcomePage);
            Write("EnabledMetric", EnabledMetric);
            Write("EnabledContinuousAssessment", EnabledContinuousAssessment);
            Write("CustomerEmail", CustomerEmail);
            Write("TargetFramework", TargetFramework);
            Write("ApplyPortAction", ApplyPortAction);
        }

        public void LoadingAllSettings()
        {
            ShowWelcomePage = (bool)Read("ShowWelcomePage", true);
            EnabledMetric = (bool)Read("EnabledMetric", true);
            EnabledContinuousAssessment = (bool)Read("EnabledContinuousAssessment", false);
            CustomerEmail = (string)Read("CustomerEmail", "customer@email.com");
            ApplyPortAction = (bool)Read("ApplyPortAction", false);
            TargetFramework = (string)Read("TargetFramework", TargetFrameworkType.NO_SELECTION);
        }

        private object Read(string property, object defaultValue)
        {
            ArgumentNotNull(property, nameof(property));
            ArgumentNotEmptyString(property, nameof(property));

            var collection = Constants.ApplicationName;
            _settingStore.CreateCollection(collection);

            if (defaultValue is bool)
                return _settingStore.GetBoolean(collection, property, (bool)defaultValue);
            else if (defaultValue is int)
                return _settingStore.GetInt32(collection, property, (int)defaultValue);
            else if (defaultValue is uint)
                return _settingStore.GetUInt32(collection, property, (uint)defaultValue);
            else if (defaultValue is long)
                return _settingStore.GetInt64(collection, property, (long)defaultValue);
            else if (defaultValue is ulong)
                return _settingStore.GetUInt64(collection, property, (ulong)defaultValue);
            return _settingStore.GetString(collection, property, defaultValue?.ToString() ?? "");
        }

        private void Write(string property, object value)
        {
            ArgumentNotNull(property, nameof(property));
            ArgumentNotEmptyString(property, nameof(property));

            var collection = Constants.ApplicationName;
            _settingStore.CreateCollection(collection);

            if (value is bool)
                _settingStore.SetBoolean(collection, property, (bool)value);
            else if (value is int)
                _settingStore.SetInt32(collection, property, (int)value);
            else if (value is uint)
                _settingStore.SetUInt32(collection, property, (uint)value);
            else if (value is long)
                _settingStore.SetInt64(collection, property, (long)value);
            else if (value is ulong)
                _settingStore.SetUInt64(collection, property, (ulong)value);
            else
                _settingStore.SetString(collection, property, value?.ToString() ?? "");
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
