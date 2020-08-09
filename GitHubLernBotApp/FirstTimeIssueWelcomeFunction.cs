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
using GitHubLernBotApp.Model;
using System.Net.Http;

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
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
            ILogger logger)
        {
            GitHubWebHookEvent webHookRequest = null;
            try
            {
                webHookRequest = new GitHubWebHookEvent(_gitHubConfiguration.WebHookSecret, req.Headers, req.Content);
            }
            catch (InvalidOperationException ioex)
            {
                logger.LogError(ioex, "Could not create instance of GitHubWebHookEvent!");
                return new BadRequestObjectResult("This only works from github webhooks!");
            }
            logger.LogInformation($"Webhook delivery: Delivery id = '{webHookRequest.DeliveryId}', Event name = '{webHookRequest.EventName}'");

            string responseMessage = $"Webhook delivery: Delivery id = '{webHookRequest.DeliveryId}', Event name = '{webHookRequest.EventName}'";

            return new OkObjectResult(responseMessage);
        }
    }
}
