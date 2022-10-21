using System;
using System.Collections.Generic;
using System.Linq;

namespace PortingAssistantExtensionClientShared.Models
{
    /// <summary>
    /// Ideal way is to move this file in PortingAssistantClient,
    /// so all Standalone and IDE extensions can share the same definitions.
    /// Due to IDE extension Client is .NetFramework, while PortingAssitantClient is .NET Core 6.0,
    /// We have to copy this file in all places.
    /// </summary>
    public class SupportedVersion : IComparable<SupportedVersion>
    {
        public string DisplayName { get; set; }
        public string VersionKey { get; set; }
        public string RequiredVisualStudioVersion { get; set; }
        public int RecommendOrder { get; set; }

        public SupportedVersion()
        { }

        public SupportedVersion(SupportedVersion other)
        {
            DisplayName = other.DisplayName;
            VersionKey = other.VersionKey;
            RequiredVisualStudioVersion = other.RequiredVisualStudioVersion;
            RecommendOrder = other.RecommendOrder;
        }

        public int CompareTo(SupportedVersion other)
        {
            return this.RecommendOrder.CompareTo(other.RecommendOrder);
        }
    }

    public class SupportedVersionConfiguration
    {
        public const string S3FilePath = "https://s3.us-west-2.amazonaws.com/mingxue-global-test/PAConfigurations/SupportedVersion.json";
        public string FormatVersion { get; set; }
        public List<SupportedVersion> Versions { get; set; }

        public SupportedVersionConfiguration()
        {
            Versions = new List<SupportedVersion>();
        }

        public string GetDisplayName(string versionKey)
        {
            return Versions.FirstOrDefault(v => v.VersionKey == versionKey)?.DisplayName;
        }

        public string GetVersionKey(string displayName)
        {
            return Versions.FirstOrDefault(v => v.DisplayName == displayName)?.VersionKey;
        }

        public SupportedVersionConfiguration DeepCopy()
        {
            var result = new SupportedVersionConfiguration()
            {
                FormatVersion = this.FormatVersion,
                Versions = new List<SupportedVersion>(),
            };

            this.Versions.ForEach(v =>
            {
                result.Versions.Add(new SupportedVersion(v));
            });

            return result;
        }
    }
}
