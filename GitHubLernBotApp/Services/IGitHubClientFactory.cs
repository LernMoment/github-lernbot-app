using Octokit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitHubLernBotApp.Services
{
    public interface IGitHubClientFactory
    {
        Task<GitHubClient> GetInstallationClient(long installationId);
    }
}
