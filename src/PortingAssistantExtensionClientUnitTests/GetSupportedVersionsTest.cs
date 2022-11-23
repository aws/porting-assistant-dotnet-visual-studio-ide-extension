using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Moq;
using Amazon.S3;
using Amazon.Runtime;
using Amazon;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using PortingAssistantVSExtensionClient.Utils;
using Amazon.S3.Model;

namespace PortingAssitantExtensionClientUnitTests
{
    [TestClass]
    public class GetSupportedVersionsTest
    {
        private string S3BucketName = "";
        private string S3File = "";
        private string BucketOwnerId = "";
        private string SupportedVersionConfig = @"
        {
            ""FormatVersion"": ""1.0"",
            ""Versions"": [
                {
                    ""DisplayName"": "".NET 6 (Microsoft LTS)"",
                    ""TargetFrameworkMoniker"": ""net6.0"",
                    ""RequiredVisualStudioVersion"": ""17.0.0"",
                    ""RecommendOrder"": ""1""
                }
            ]
        }";

        private Mock<AmazonS3Client> _mockAmazonS3Client;

        [TestInitialize]
        public void OneTimeSetup()
        {
            _mockAmazonS3Client = new Mock<AmazonS3Client>(
                FallbackCredentialsFactory.GetCredentials(true),
                RegionEndpoint.USEast2);
        }


        [TestMethod]
        public async Task Test_GetSupportedVersions_WithoutS3Response()
        {
            _mockAmazonS3Client.Reset();

            var result = await SupportedVersionsUtil.Instance.GetSupportedConfigurationAsync(
                _mockAmazonS3Client.Object,
                S3BucketName,
                S3File,
                BucketOwnerId,
                false);

            Assert.Multiple(() =>
            {
                Assert.That(result.Item1, Is.Not.Null);
                Assert.That(result.Item2.Contains("Porting Assistant failed to configure supported versions"));
            });
        }

        [TestMethod]
        public async Task Test_GetSupportedVersions_WithBadS3Response()
        {
            _mockAmazonS3Client.Reset();
            _mockAmazonS3Client
                .Setup(client => client.GetObjectAsync(
                    It.Is<GetObjectRequest>(r => r.BucketName == S3BucketName && r.Key == S3File),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetObjectRequest request, CancellationToken ct) =>
                {
                    return new GetObjectResponse()
                    {
                        ResponseStream = null,
                    };
                });

            var result = await SupportedVersionsUtil.Instance.GetSupportedConfigurationAsync(
                _mockAmazonS3Client.Object,
                S3BucketName,
                S3File,
                BucketOwnerId,
                false);

            Assert.Multiple(() =>
            {
                Assert.That(result.Item1, Is.Not.Null);
                Assert.That(result.Item2.Contains("Porting Assistant failed to configure supported versions"));
            });
        }

        [TestMethod]
        public async Task Test_GetSupportedVersions_WithExpectedS3Response()
        {
            _mockAmazonS3Client.Reset();

            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);
            writer.Write(SupportedVersionConfig);
            writer.Flush();
            memoryStream.Position = 0;

            _mockAmazonS3Client
                .Setup(client => client.GetObjectAsync(
                    It.Is<GetObjectRequest>(r => r.BucketName == S3BucketName && r.Key == S3File),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetObjectRequest request, CancellationToken ct) =>
                {
                    return new GetObjectResponse()
                    {
                        ResponseStream = memoryStream,
                    };
                });

            var result = await SupportedVersionsUtil.Instance.GetSupportedConfigurationAsync(
                _mockAmazonS3Client.Object,
                S3BucketName,
                S3File,
                BucketOwnerId,
                false);

            Assert.Multiple(() =>
            {
                Assert.That(result.Item1, Is.Not.Null);
                Assert.That(result.Item1.FormatVersion, Is.EqualTo("1.0"));
                Assert.That(result.Item1.Versions, Is.Not.Null);
                Assert.That(result.Item1.Versions, Has.Count.EqualTo(1));
                Assert.That(string.IsNullOrEmpty(result.Item2));
            });
        }

        [TestMethod]
        public async Task Test_GetSupportedVersions_WithNotFoundS3Exception()
        {
            _mockAmazonS3Client.Reset();

            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);
            writer.Write(SupportedVersionConfig);
            writer.Flush();
            memoryStream.Position = 0;

            _mockAmazonS3Client
                .Setup(client => client.GetObjectAsync(
                    It.Is<GetObjectRequest>(r => r.BucketName == S3BucketName && r.Key == S3File),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetObjectRequest request, CancellationToken ct) =>
                {
                    throw new AmazonS3Exception("")
                    {
                        StatusCode = System.Net.HttpStatusCode.NotFound
                    };
                });

            var result = await SupportedVersionsUtil.Instance.GetSupportedConfigurationAsync(
                _mockAmazonS3Client.Object,
                S3BucketName,
                S3File,
                BucketOwnerId,
                false);

            Assert.Multiple(() =>
            {
                Assert.That(result.Item1, Is.Not.Null);
                Assert.That(result.Item2.Contains("The supported version configuration file is not available"));
            });
        }
    }
}
