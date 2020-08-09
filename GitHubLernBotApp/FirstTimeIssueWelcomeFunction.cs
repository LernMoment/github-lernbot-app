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
using System.Net;
using Octokit;
using GitHubLernBotApp.Services;
using System.Linq;
using System.Text;

namespace GitHubLernBotApp
{
    public class FirstTimeIssueWelcomeFunction
    {
        private readonly GitHubConnectionOptions _gitHubConfiguration;
        private readonly IGitHubClientFactory _clientFactory;

        public FirstTimeIssueWelcomeFunction(IOptions<GitHubConnectionOptions> config, IGitHubClientFactory clienFactory)
        {
            _gitHubConfiguration = config.Value;
            _clientFactory = clienFactory;
        }

        [FunctionName("FirstTimeIssueWelcome")]
        public async Task<HttpResponseMessage> Run(
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
                        var client = await _clientFactory.GetInstallationClient(payloadObject.Installation.Id);

                        var creatorName = payloadObject.Issue.User.Login;
                        var respositoryName = payloadObject.Repository.Name;
                        var ownerName = payloadObject.Repository.Owner.Login;

                        var allIssuesForUser = new RepositoryIssueRequest
                        {
                            Creator = creatorName,
                            State = ItemStateFilter.All,
                            Filter = IssueFilter.All
                        };

                        var issues = await client.Issue.GetAllForRepository(ownerName, respositoryName, allIssuesForUser);
                        var issueCountForCreator = issues.Where(i => i.PullRequest == null).Count();
                        if (issueCountForCreator == 1)
                        {
                            var welcomeFileResponse = await client.Repository.Content.GetRawContent(ownerName, respositoryName, ".github/welcome-first-issue.md");
                            var welcomeFileContent = $"@{creatorName} " + Encoding.Default.GetString(welcomeFileResponse);

                            var issueNumber = payloadObject.Issue.Number;
                            var repositoryId = payloadObject.Repository.Id;
                            _ = await client
                                        .Issue.Comment
                                        .Create(repositoryId, issueNumber, welcomeFileContent);

                            logger.LogInformation($"Commented Issue: '{issueNumber}' with this message: '{welcomeFileContent}'");
                        }
                        else
                        {
                            logger.LogInformation($"Issue-Creator: '{creatorName}' is not a first time contributor!");
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
