using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace GitHubLernBotApp
{
    public class FirstTimeIssueWelcomeFunction
    {
        private readonly GitHubConnectionOptions _gitHubConfiguration;

        public FirstTimeIssueWelcomeFunction(IOptions<GitHubConnectionOptions> config)
        {
            _gitHubConfiguration = config.Value;
        }

        [FunctionName("FirstTimeIssueWelcome")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string responseMessage = $"Welcome to '{_gitHubConfiguration.AppName}'";

            return new OkObjectResult(responseMessage);
        }
    }
}
