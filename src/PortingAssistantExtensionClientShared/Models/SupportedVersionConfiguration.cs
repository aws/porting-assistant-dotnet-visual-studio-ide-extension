using PortingAssistantVSExtensionClient.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortingAssistantVSExtensionClient.Models
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
        public string TargetFrameworkMoniker { get; set; }
        public string RequiredVisualStudioVersion { get; set; }
        public string RecommendOrder { get; set; }

        public SupportedVersion()
        { }

        public SupportedVersion(SupportedVersion other)
        {
            DisplayName = other.DisplayName;
            TargetFrameworkMoniker = other.TargetFrameworkMoniker;
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
        public string FormatVersion { get; set; }
        public List<SupportedVersion> Versions { get; set; }
        public SupportedVersionConfiguration()
        {
            Versions = new List<SupportedVersion>();
        }

        public static SupportedVersionConfiguration GetDefaultConfiguration()
        {
            return new SupportedVersionConfiguration()
            {
                FormatVersion = "1.0",
                Versions = new List<SupportedVersion>()
                {
                    new SupportedVersion()
                    {
                        DisplayName = ".NET 7 (Standard Term Support)",
                        TargetFrameworkMoniker = TargetFrameworkType.NET70,
                        RequiredVisualStudioVersion = "17.4.0",
                        RecommendOrder = "1",
                    },
                    new SupportedVersion()
                    {
                        DisplayName = ".NET 6 (Microsoft LTS)",
                        TargetFrameworkMoniker = TargetFrameworkType.NET60,
                        RequiredVisualStudioVersion = "17.0.0",
                        RecommendOrder = "2",
                    },
                    new SupportedVersion()
                    {
                        DisplayName = ".NET Core 3.1 (Microsoft LTS)",
                        TargetFrameworkMoniker = TargetFrameworkType.NETCOREAPP31,
                        RequiredVisualStudioVersion = "16.0.0",
                        RecommendOrder = "3",
                    },
                    new SupportedVersion()
                    {
                        DisplayName = ".NET 5 (Microsoft out of support)",
                        TargetFrameworkMoniker = TargetFrameworkType.NET50,
                        RequiredVisualStudioVersion = "16.0.0",
                        RecommendOrder = "4",
                    },
                }
            };
        }

        public string GetDisplayName(string versionKey)
        {
            return Versions.FirstOrDefault(v => v.TargetFrameworkMoniker == versionKey)?.DisplayName;
        }

        public string GetVersionKey(string displayName)
        {
            return Versions.FirstOrDefault(v => v.DisplayName == displayName)?.TargetFrameworkMoniker;
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
