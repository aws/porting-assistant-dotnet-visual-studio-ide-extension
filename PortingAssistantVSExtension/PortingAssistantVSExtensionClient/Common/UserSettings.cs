using PortingAssistantVSExtensionClient.Common;
using System;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using SettingsStore = PortingAssistantVSExtensionClient.Common.SettingsStore;
using PortingAssistantVSExtensionClient.Models;

namespace PortingAssistantVSExtensionClient.Options
{
    public sealed class UserSettings
    {
        private static SettingsStore settingsStore;

        private static UserSettings instance = null;

        public bool EnableMetric;
        public bool EnableAutoAssessment;
        public string CustomerEmail;
        public TargetFrameworkType TargetFramework;




        public UserSettings(IServiceProvider serviceProvider)
        {
            var sm = new ShellSettingsManager(serviceProvider);
            settingsStore = new SettingsStore(sm.GetWritableSettingsStore(SettingsScope.UserSettings), Constants.ApplicationName);
        }

        public static void Create(IServiceProvider serviceProvider)
        {
            if (instance != null) return;
            instance = new UserSettings(serviceProvider);
            instance.LoadingAllSettings();
        }

        public static UserSettings Instance => instance;

        public T ReadValue<T>(string property, string value)
        {
            return (T)settingsStore.Read(property, value);
        }

        public void WriteValue(string property, string value)
        {
            settingsStore.Write(property, value);
        }

        public void SaveAllSettings()
        {
            settingsStore.Write("EnableMetric", EnableMetric);
            settingsStore.Write("EnableAutoAssessment", EnableAutoAssessment);
            settingsStore.Write("CustomerEmail", CustomerEmail);
            settingsStore.Write("TargetFramework", TargetFramework);

        }

        public void LoadingAllSettings()
        {
            EnableMetric = (bool)settingsStore.Read("EnableMetric", true);
            EnableAutoAssessment = (bool)settingsStore.Read("EnableAutoAssessment", false);
            CustomerEmail = (string)settingsStore.Read("CustomerEmail", "customer@email.com");
            TargetFramework = (TargetFrameworkType)Enum.Parse(typeof(TargetFrameworkType),(string)settingsStore.Read("TargetFramework", TargetFrameworkType.no_selection.ToString()));
        }

    }
}
