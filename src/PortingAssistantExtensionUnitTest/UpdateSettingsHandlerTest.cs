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

        private readonly UpdateSettingsRequest _updateSettingsRequest = new UpdateSettingsRequest
        {
            AWSProfileName = "testProfileUpdate",
            EnabledContinuousAssessment = true,
            EnabledMetrics = true
        };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _logger = new Mock<ILogger<UpdateSettingsHandler>>();
            _updateSettingsHandler = new UpdateSettingsHandler(_logger.Object);
        }

        [Test]
        public async Task UpdateSettingsHandlerSuccessAsync()
        {
            var result = await _updateSettingsHandler.Handle(_updateSettingsRequest, CancellationToken.None);

            Assert.IsTrue(result);
            Assert.IsTrue(PALanguageServerConfiguration.EnabledMetrics);
            Assert.IsTrue(PALanguageServerConfiguration.EnabledContinuousAssessment);
            Assert.AreEqual(PALanguageServerConfiguration.AWSProfileName, "testProfileUpdate");
        }

    }
}
