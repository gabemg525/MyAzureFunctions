using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;

namespace Visitor.Function
{
    public class VisitorCount
    {
        private readonly ILogger<VisitorCount> _logger;

        public VisitorCount(ILogger<VisitorCount> logger)
        {
            _logger = logger;
        }

        [Function("VisitorCount")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Authenticate using DefaultAzureCredential
            TokenCredential credential = new DefaultAzureCredential();

            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
