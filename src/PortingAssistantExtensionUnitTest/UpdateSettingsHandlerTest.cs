using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PortingAssistantExtensionServer.Common;
using PortingAssistantExtensionServer.Handlers;
using PortingAssistantExtensionServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortingAssistantExtensionUnitTest
{
    public class UpdateSettingsHandlerTest
    {
        private Mock<ILogger<UpdateSettingsHandler>> _logger;
        private UpdateSettingsHandler _updateSettingsHandler;
        private UpdateSettingsHandler _updateSettingsHandlerWithDefaultCreds;

        private readonly UpdateSettingsRequest _updateSettingsRequest = new UpdateSettingsRequest
        {
            AWSProfileName = "testProfileUpdate",
            EnabledContinuousAssessment = true,
            EnabledMetrics = true,
        };

        private readonly UpdateSettingsRequest _updateSettingsRequestWithDefaultCreds = new UpdateSettingsRequest
        {
            AWSProfileName = "testProfileUpdate",
            EnabledContinuousAssessment = true,
            EnabledMetrics = true,
            EnabledDefaultCredentials = true,
        };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _logger = new Mock<ILogger<UpdateSettingsHandler>>();
            _updateSettingsHandler = new UpdateSettingsHandler(_logger.Object);
            _updateSettingsHandlerWithDefaultCreds = new UpdateSettingsHandler(_logger.Object);
        }

        [Test]
        public async Task UpdateSettingsHandlerSuccessAsync()
        {
            var result = await _updateSettingsHandler.Handle(_updateSettingsRequest, CancellationToken.None);

            Assert.IsTrue(result);
            Assert.IsTrue(PALanguageServerConfiguration.EnabledMetrics);
            Assert.IsTrue(PALanguageServerConfiguration.EnabledContinuousAssessment);
            Assert.IsFalse(PALanguageServerConfiguration.EnabledDefaultCredentials);
            Assert.AreEqual(PALanguageServerConfiguration.AWSProfileName, "testProfileUpdate");
        }

        [Test]
        public async Task UpdateSettingsHandlerWithDefaultCredsSuccessAsync()
        {
            var result = await _updateSettingsHandlerWithDefaultCreds.Handle(_updateSettingsRequestWithDefaultCreds, CancellationToken.None);

            Assert.IsTrue(result);
            Assert.IsTrue(PALanguageServerConfiguration.EnabledMetrics);
            Assert.IsTrue(PALanguageServerConfiguration.EnabledContinuousAssessment);
            Assert.IsTrue(PALanguageServerConfiguration.EnabledDefaultCredentials);
            Assert.AreEqual(PALanguageServerConfiguration.AWSProfileName, "testProfileUpdate");
        }

    }
}
