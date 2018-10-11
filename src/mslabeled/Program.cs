using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Octokit;
using Octokit.Internal;
using Serilog;

namespace mslabeled
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Ready? Hit Enter.");
            Console.ReadLine();

            try
            {
                DoGitHubStuff().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex.Message}");
                Console.ReadLine();
            }
        }

        private static async Task DoGitHubStuff()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddUserSecrets(Assembly.GetExecutingAssembly()).Build();

            var logger = new LoggerConfiguration()
                .WriteTo.Seq("http://localhost:5341")
                .MinimumLevel.Verbose()
                .Enrich.WithProperty("ApplicationName", "MSLabeled")
                .CreateLogger();

            logger.Information("Starting");

            var login = new Credentials(config["GitHubUser"], config["GitHubToken"]);
            var credentialStore = new InMemoryCredentialStore(login);
            var github = new GitHubClient(
                new ProductHeaderValue("MSLabeled"),
                credentialStore);

            var repos = await github.Repository.GetAllForOrg("Microsoft");

            var i = 0;
            foreach (var repo in repos)
            {
                i++;
                var labels = await github.Issue.Labels.GetAllForRepository(repo.Id);
                var potentiallyMatchingLabels = labels
                    .Where(x => LabelIsPotentiallyUpForGrabs(x.Name))
                    .Select(lab => lab.Name)
                    .ToList();

                var openIssuesWithMatchingLabels = new RepositoryIssueRequest
                {
                    State = ItemStateFilter.Open,
                    Filter = IssueFilter.All
                };

                potentiallyMatchingLabels.ForEach(x => openIssuesWithMatchingLabels.Labels.Add(x));

                var issuesWithMatchingLabels = await github.Issue.GetAllForRepository(repo.Id, openIssuesWithMatchingLabels);
                logger.Debug("Repo {repoName} has {totalIssueCount} total potentially matching issues according to Octokit.", repo.FullName, issuesWithMatchingLabels.Count);

                if (issuesWithMatchingLabels.Count > 0)
                {
                    potentiallyMatchingLabels
                        .ForEach(match =>
                        {
                            var matchingIssues = issuesWithMatchingLabels
                                .Where(x => x.Labels.Any(lab => lab.Name == match)).ToList();
                            logger.Information(
                                "Repo {repoName} with {starCount} has label {labelName} with {issueCount} issues",
                                repo.FullName, repo.StargazersCount, match, matchingIssues.Count);
                        });
                }
                else
                {
                    logger.Debug("Skipping {repoName} because there are no potentially matching issues", repo.FullName);
                }

                CheckApiInfo(github.GetLastApiInfo(), logger);
                if (i % 20 == 0) { logger.Verbose("Checked {numberOfRepos} repositories so far", i); }
            }
        }

        private static void CheckApiInfo(ApiInfo apiInfo, ILogger logger)
        {
            if (apiInfo.RateLimit.Remaining % 20 == 0)
            {
                logger.Debug("Rate limit info: {@rateLimit}", apiInfo.RateLimit);
            }
            if (apiInfo.RateLimit.Remaining <= 1)
            {
                throw new Exception($"Rate limit reached; you need to stop until {apiInfo.RateLimit.Reset}");
            }
        }

        private static bool LabelIsPotentiallyUpForGrabs(string name)
        {
            return ThingsToSearch.Any(x => name.Contains(x, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <remarks>Brainstormed from up-for-grabs.net</remarks>
        private static readonly List<string> ThingsToSearch = new List<string>
        {
            // Used jQuery on up-for-grabs: $("p.label > a").each(function(index){console.log($(this).text())})
            "up for",
            "up-for",
            "first",
            "help",
            "bitesize",
            "teaser",
            "easy",
            "novice",
            "trivial",
            "junior",
            "jump",
            "first",
            "newbie",
            "quick",
            "welcome",
            "low-hanging",
            "low hanging",
            "low_hanging",
            "you take it",
            "new",
            "accepting"
        };
    }
}
