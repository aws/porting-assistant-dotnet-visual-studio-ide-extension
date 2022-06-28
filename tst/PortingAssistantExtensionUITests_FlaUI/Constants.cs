using System;
using System.Collections.Generic;
using System.Text;

namespace IDE_UITest
{
    public static class Constants
    {
        public const string RunFullAssessmentsMenuItem = "Run Full Assessment with Porting Assistant";
        public const string EnableIncrementalAssessmentsMenuItem = "Enable Incremental Assessments with Porting Assistant";
        public const string RunFullSolutionPortingMenuItem = "Port Solution to .NET Core with Porting Assistant";
        public static string RunSingleProjectPortingMenuItem = "Port Project to .NET Core with Porting Assistant";
        public const string TestOnAWSMenuItem = "Test Applications on AWS";
        public const string ViewRunningApplicationsOnAWSMenuItem = "View Running Applications on AWS";

        public enum Version 
        { 
            VS2019,
            VS2022
        }
    }
}
