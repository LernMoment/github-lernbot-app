using GitHubLernBotApp.Model;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubLernBotApp.Services
{
    class LernBot : ILernBot
    {
        private const string _textFilePath = ".github";
        private const string _firstIssueWelcomeFileName = "welcome-first-issue.md";

        private readonly IGitHubClientFactory _clientFactory;

        public LernBot(IGitHubClientFactory clienFactory)
        {
            _clientFactory = clienFactory;
        }

        public async Task<bool> WelcomeUserIfFirstTimeContributor(long installationId, Issue issue, Repository repo)
        {
            var isFirstTimeUser = false;

            var client = await _clientFactory.GetInstallationClient(installationId);

            int issueCountForCreator = await GetAmountOfIssuesForUser(client, issue.User.Login, repo);

            if (issueCountForCreator == 1)
            {
                await PostWelcomeMessage(client, issue, repo);
                isFirstTimeUser = true;
            }

            return isFirstTimeUser;
        }

        private async Task<int> GetAmountOfIssuesForUser(GitHubClient client, string creatorName, Repository repo)
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
