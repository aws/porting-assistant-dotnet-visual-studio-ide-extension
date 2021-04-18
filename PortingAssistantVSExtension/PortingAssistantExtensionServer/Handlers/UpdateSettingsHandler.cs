using OmniSharp.Extensions.JsonRpc;
using PortingAssistantExtensionServer.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using MediatR;
using PortingAssistantExtensionServer.Common;

namespace PortingAssistantExtensionServer.Handlers
{
    [Serial, Method("updateSettings")]
    internal interface IUpdateSettingsHandler : IJsonRpcRequestHandler<UpdateSettingsRequest, bool> { }
    class UpdateSettingsHandler : IUpdateSettingsHandler
    {
        private readonly ILogger _logger;
        public UpdateSettingsHandler(ILogger<SolutionAssessmentHandler> logger)
        {
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateSettingsRequest request, CancellationToken cancellationToken)
        {
            if(request.AWSProfileName != null)
                PALanguageServerConfiguration.AWSProfileName = request.AWSProfileName;
            if (request.RootCacheFolder != null)
                PALanguageServerConfiguration.RootCacheFolder = request.RootCacheFolder;
            PALanguageServerConfiguration.EnabledContinuousAssessment = request.EnabledContinuousAssessment;
            PALanguageServerConfiguration.EnabledMetrics = request.EnabledMetrics;
            return true;
        }
    }
}
