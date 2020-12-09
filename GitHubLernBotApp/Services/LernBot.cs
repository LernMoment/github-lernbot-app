using Octokit;
using System;
using System.Threading.Tasks;

namespace GitHubLernBotApp.Services
{
    class LernBot : ILernBot
    {
        public Task WelcomeUserIfFirstTimeContributor(long installationId, Issue issue, Repository repo)
        {
            throw new NotImplementedException();
        }
    }
}
