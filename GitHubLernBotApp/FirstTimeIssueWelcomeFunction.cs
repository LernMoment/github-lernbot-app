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
        private const string _textFilePath = ".github";
        private const string _firstIssueWelcomeFileName = "welcome-first-issue.md"; 
        private readonly GitHubConnectionOptions _gitHubConfiguration;
        private readonly IGitHubClientFactory _clientFactory;

        public FirstTimeIssueWelcomeFunction(IOptions<GitHubConnectionOptions> config, IGitHubClientFactory clienFactory)
        {
            _gitHubConfiguration = config.Value;
            _clientFactory = clienFactory;
        }

        [FunctionName("FirstTimeIssueWelcome")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
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

                        int issueCountForCreator = await GetAmountOfIssuesForUser(client, payloadObject.Issue.User.Login, payloadObject.Repository);

                        if (issueCountForCreator == 1)
                        {
                            await PostWelcomeMessage(client, payloadObject.Issue, payloadObject.Repository);
                        }
                        else
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

        private static async Task<int> GetAmountOfIssuesForUser(GitHubClient client, string creatorName, Repository repo)
        {
            var allIssuesForUser = new RepositoryIssueRequest
            {
                Creator = creatorName,
                State = ItemStateFilter.All,
                Filter = IssueFilter.All
            };

            var issues = await client.Issue.GetAllForRepository(repo.Owner.Login, repo.Name, allIssuesForUser);

            // PullRequest are also Issues, but we are only looking for "real" issues
            return issues.Where(i => i.PullRequest == null).Count();
        }

        private async Task PostWelcomeMessage(GitHubClient client, Issue issue, Repository repo)
        {
            var welcomeFileResponse = await client.Repository.Content.GetRawContent(
                repo.Owner.Login, 
                repo.Name, 
                $"{_textFilePath}/{_firstIssueWelcomeFileName}");
            
            var welcomeFileContent = $"@{issue.User.Login} " + Encoding.Default.GetString(welcomeFileResponse);

            _ = await client
                        .Issue.Comment
                        .Create(repo.Id, issue.Number, welcomeFileContent);
            return;
        }
    }
}
