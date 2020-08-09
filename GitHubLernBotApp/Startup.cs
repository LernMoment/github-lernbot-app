using GitHubLernBotApp.Model;
using GitHubLernBotApp.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

[assembly: FunctionsStartup(typeof(GitHubLernBotApp.Startup))]

namespace GitHubLernBotApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Prepare the "configuration" based on user secrets
            var config = new ConfigurationBuilder()
               .SetBasePath(Environment.CurrentDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            builder.Services.AddSingleton<IConfiguration>(config);

            // Inject configuration wich should be read from user secrets
            builder
                .Services
                .AddOptions<GitHubConnectionOptions>()
                .Configure<IConfiguration>((gitHubSettings, configuration) =>
                {
                    configuration
                    .GetSection(GitHubConnectionOptions.GitHub)
                    .Bind(gitHubSettings);
                });

            builder.Services.AddScoped<IGitHubClientFactory, GitHubClientFactory>();
        }
    }
}
