using System;
using System.Collections.Generic;
using System.Text;

namespace GitHubLernBotApp
{
    public class GitHubConnectionOptions
    {
        public const string GitHub = "GitHub";

        public string WebHookSecret { get; set; }

        public int AppId { get; set; }

        public string PrivateKey { get; set; }

        public string AppName { get; set; }
    }
}
