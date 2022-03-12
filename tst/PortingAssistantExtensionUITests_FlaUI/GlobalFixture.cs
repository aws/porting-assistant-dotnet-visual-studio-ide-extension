using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
[assembly: TestCollectionOrderer("IDE_UITest.CustomTestCollectionOrderer", "IDE_UITest")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace IDE_UITest
{
    public class GlobalFixture : IDisposable
    {
        public IConfigurationSection InputData { get; set; }
        public IConfigurationSection AwsAuth { get; set; }
        public string Vs2019Location  { get; set; }
        public string Vs2022Location { get; set; }
        public GlobalFixture()
        {
            // Called once before running all tests in IDE_UITest
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .Build();
            InputData = config.GetSection("input");
            AwsAuth = config.GetSection("aws");
            var vslocations = config.GetSection("vslocation");
            Vs2019Location = vslocations["2019"];
            Vs2022Location = vslocations["2022"];
        }
        
        public void Dispose()
        {
            //shut down visual studio
            
        }
    }

    // Define the collection with the fixture
    [CollectionDefinition("Collection1")]
    public class Collection1Class : ICollectionFixture<GlobalFixture> { }

    [CollectionDefinition("Collection2")]
    public class Collection2Class : ICollectionFixture<GlobalFixture> { }

    public class CustomTestCollectionOrderer : ITestCollectionOrderer
    {
        public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
        {
            return testCollections.OrderBy(collection => collection.DisplayName);
        }
    }

    public class CustomTestCaseOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
        {
            return testCases.OrderByDescending(test => test.DisplayName);
        }
    }

}
