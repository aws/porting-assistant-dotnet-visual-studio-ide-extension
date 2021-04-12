﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistantExtensionServer.Common
{
    public static class PALanguageServerConfiguration
    {
        static bool _enabledContinuousAssessment { get; set; }

        static bool _enabledMetrics { get; set; }

        static string _customerEmail { get; set; }

        static string _rootCacheFolder { get; set; }
        static PALanguageServerConfiguration()
        {
            _enabledContinuousAssessment = false;
            _enabledMetrics = false;
            _customerEmail = "";
            _rootCacheFolder = "";
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

        public static string CustomerEmail
        {
            get
            {
                return _customerEmail;
            }
            set
            {
                _customerEmail = value;
            }
        }

        public static string RootCacheFolder
        {
            get
            {
                return _rootCacheFolder;
            }
            set
            {
                _rootCacheFolder = value;
            }
        }

    }
}