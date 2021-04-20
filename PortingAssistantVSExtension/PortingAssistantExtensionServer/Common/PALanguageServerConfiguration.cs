﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Common
{
    public static class PALanguageServerConfiguration
    {
        static bool _enabledContinuousAssessment { get; set; }

        static bool _enabledMetrics { get; set; }

        static string _awsProfileName { get; set; }

        static string _extensionVersion { get; set; }

        static string _extensionType { get; set; }

        static PALanguageServerConfiguration()
        {
            _enabledContinuousAssessment = false;
            _enabledMetrics = false;
            _awsProfileName = "";
            _extensionVersion = "";
        }

        public static bool EnabledContinuousAssessment
        {
            get
            {
                return _enabledContinuousAssessment;
            }
            set
            {
                _enabledContinuousAssessment = value;
            }
        }

        public static bool EnabledMetrics
        {
            get
            {
                return _enabledMetrics;
            }
            set
            {
                _enabledMetrics = value;
            }
        }

        public static string AWSProfileName
        {
            get
            {
                return _awsProfileName;
            }
            set
            {
                _awsProfileName = value;
            }
        }

        public static string ExtensionVersion
        {
            get
            {
                return _extensionVersion;
            }
            set
            {
                _extensionVersion = value;
            }
        }

        public static string ExtensionType
        {
            get
            {
                return _extensionType;
            }
            set
            {
                _extensionType = value;
            }
        }

    }
}
