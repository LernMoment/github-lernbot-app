using GitHubLernBotApp.Model;
using Microsoft.Extensions.Options;
using Octokit;
using Octokit.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitHubLernBotApp.Services
{
    public class GitHubClientFactory : IGitHubClientFactory
    {
        private readonly GitHubConnectionOptions _gitHubConfiguration;
        public GitHubClientFactory(IOptions<GitHubConnectionOptions> options)
        {
            _gitHubConfiguration = options.Value;
        }

        public async Task<GitHubClient> GetInstallationClient(long installationId)
        {
            var accessToken = await GetAppClient().GitHubApps.CreateInstallationToken(installationId);

            return new ResilientGitHubClientFactory()
                .Create(new ProductHeaderValue($"{_gitHubConfiguration.AppName}-Installation{installationId}"), new Credentials(accessToken.Token), new InMemoryCacheProvider());
        }

        private GitHubClient GetAppClient()
        {
            // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
            var generator = new GitHubJwt.GitHubJwtFactory(
                new GitHubJwt.StringPrivateKeySource(_gitHubConfiguration.PrivateKey),
                new GitHubJwt.GitHubJwtFactoryOptions
                {
                    AppIntegrationId = _gitHubConfiguration.AppId, // The GitHub App Id
                ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
            }
            );
            var jwtToken = generator.CreateEncodedJwtToken();

            return GetGitHubClient(_gitHubConfiguration.AppName, jwtToken);
        }

        private static GitHubClient GetGitHubClient(string appName, string jwtToken)
        {
            return new ResilientGitHubClientFactory()
                .Create(new ProductHeaderValue(appName), new Credentials(jwtToken, AuthenticationType.Bearer), new InMemoryCacheProvider());
        }
    }
}
