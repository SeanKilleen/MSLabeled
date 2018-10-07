using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var logger = new LoggerConfiguration()
                .WriteTo.Seq("http://localhost:5341")
                .MinimumLevel.Verbose()
                .Enrich.WithProperty("ApplicationName", "MSLabeled")
                .CreateLogger();

            logger.Information("Starting");

            var login = new Credentials("TODO-USERNAME", "TODO-ACCESSTOKEN");
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

                labels
                    .Where(x=> LabelIsPotentiallyUpForGrabs(x.Name))
                    .ToList()
                    .ForEach(async match =>
                    {
                        var issueRequest = new RepositoryIssueRequest
                        {
                            Filter = IssueFilter.All,
                            State = ItemStateFilter.Open,
                            Labels = {match.Name}
                        };
                        var issueList = await github.Issue.GetAllForRepository(repo.Id, issueRequest);
                        logger.Information("Repo {repoName} with {starCount} has label {labelName} with {issueCount} issues", repo.FullName, repo.StargazersCount, match.Name, issueList.Count);
                    });

                CheckApiInfo(github.GetLastApiInfo(), logger);
                if(i % 20 == 0){logger.Verbose("Checked {numberOfRepos} repositories so far", i);}
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
