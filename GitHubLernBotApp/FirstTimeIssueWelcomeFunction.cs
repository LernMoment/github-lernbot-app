using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net;
using System.Linq;
using System.Text;
using GitHubLernBotApp.Model;
using GitHubLernBotApp.Services;
using Octokit;

namespace GitHubLernBotApp
{
    public class FirstTimeIssueWelcomeFunction
    {
        private readonly GitHubConnectionOptions _gitHubConfiguration;
        private readonly ILernBot _bot;

        public FirstTimeIssueWelcomeFunction(IOptions<GitHubConnectionOptions> config, ILernBot bot)
        {
            _gitHubConfiguration = config.Value;
            _bot = bot;
        }

        [FunctionName("FirstTimeIssueWelcome")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger logger)
        {
            GitHubWebHookEvent webHookRequest;
            try
            {
                webHookRequest = new GitHubWebHookEvent(_gitHubConfiguration.WebHookSecret, req.Headers, req.Content);
            }
            catch (InvalidOperationException ioex)
            {
                logger.LogError(ioex, "Could not create instance of GitHubWebHookEvent!");
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "This only works from github webhooks!");
            }
            logger.LogInformation($"Webhook delivery: Delivery id = '{webHookRequest.DeliveryId}', Event name = '{webHookRequest.EventName}'");

            if (!webHookRequest.ContainsExpectedData())
            {
                logger.LogWarning("GitHubWebHook did not contain all expected data!");
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Received incomplete headers");
            }

            if (!await webHookRequest.IsMessageAuthenticated())
            {
                logger.LogWarning("Invalid signature - Message from GitHub-WebHook could not be authenticated!");
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid signature");
            }

            if (webHookRequest.EventName == "issues")
            {
                var payloadObject = await webHookRequest.DeserializePayload<IssueEventPayload>();
                logger.LogInformation($"Received event from issue with title: '{payloadObject.Issue.Title}'");

                if (payloadObject.Action == "opened")
                {
                    try
                    {
                        var isFirstTime = await _bot.WelcomeUserIfFirstTimeContributor(payloadObject.Installation.Id, payloadObject.Issue, payloadObject.Repository);

                        if(!isFirstTime)
                        {
                            logger.LogInformation($"Issue-Creator: '{payloadObject.Issue.User.Login}' is not a first time contributor!");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Exception gefangen: {ex}");
                        return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Encountered Exception");
                    }
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
