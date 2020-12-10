using Octokit;
using System.Threading.Tasks;

namespace GitHubLernBotApp.Services
{
    public interface ILernBot
    {
        Task<bool> WelcomeUserIfFirstTimeContributor(long installationId, Issue issue, Repository repo);
    }
}
