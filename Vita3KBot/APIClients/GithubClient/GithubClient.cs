using System.Threading.Tasks;
using System.Linq;

using Discord;

using Octokit;

namespace APIClients {
    public static class GithubClient {
    public class BuildAssets
    {
      public ReleaseAsset Windows { get; init; } = null!;
      public ReleaseAsset Linux { get; init; } = null!;
      public ReleaseAsset AppImage { get; init; } = null!;
      public ReleaseAsset MacOS { get; init; } = null!;
    }

        public static async Task<Embed> GetLatestBuild() {

            GitHubClient github = new GitHubClient(new ProductHeaderValue("Vita3KBot"));
            Release latestRelease = await github.Repository.Release.Get("Vita3k", "Vita3k", "continuous");

            // Get commit and PR info
            string commit = latestRelease.Body.Substring(latestRelease.Body.IndexOf("commit:") + 7).Trim();
            commit = commit.Substring(0, commit.IndexOf("\n"));
            GitHubCommit REF = await github.Repository.Commit.Get("Vita3k", "Vita3k", commit);
            Issue prInfo = await GetPRInfo(github, commit);
            string bodyText = !string.IsNullOrWhiteSpace(prInfo.Body) ? prInfo.Body : REF.Commit.Message;

           // Get build assets
            string buildNum = latestRelease.Body.Substring(latestRelease.Body.IndexOf("Build:") + 6).Trim();
            BuildAssets assets = await GetReleaseAssets(github, buildNum, latestRelease);
            string releaseTime = $"Published at {latestRelease.PublishedAt:u}";

            EmbedBuilder LatestBuild = new EmbedBuilder();
            if (prInfo != null) {
                LatestBuild.WithTitle($"PR: #{prInfo.Number} By {prInfo.User.Login}")
                .WithUrl(prInfo.HtmlUrl);
            } else {
                LatestBuild.WithTitle($"Commit: {REF.Sha} By {REF.Commit.Author.Name}")
                .WithUrl($"https://github.com/vita3k/vita3k/commit/{REF.Sha}");
            }

            LatestBuild.WithDescription($"**{prInfo.Title}**\n\n{bodyText}")
            .WithColor(Color.Orange)
            .AddField("Windows", $"[{assets.Windows.Name}]({assets.Windows.BrowserDownloadUrl})")
            .AddField("Linux", $"[{assets.Linux.Name}]({assets.Linux.BrowserDownloadUrl}), [{assets.AppImage.Name}]({assets.AppImage.BrowserDownloadUrl})")
            .AddField("Mac", $"[{assets.MacOS.Name}]({assets.MacOS.BrowserDownloadUrl})")
            .WithFooter(releaseTime);

            return LatestBuild.Build();
        }

        private static async Task<Issue> GetPRInfo(GitHubClient github, string commit) {

            var request = new SearchIssuesRequest(commit) {
                Type = IssueTypeQualifier.PullRequest,
                State = ItemState.Closed,
            };
            request.Repos.Add("Vita3K/Vita3K");

            var searchResults = (await github.Search.SearchIssues(request)).Items;

            return searchResults.FirstOrDefault();
        }

        private static async Task<BuildAssets> GetReleaseAssets(GitHubClient github,string buildNum, Release latestRelease) {
            try {
                var storeRelease = await github.Repository.Release.Get("Vita3k","Vita3k-builds",buildNum);
                return new BuildAssets
                {
                    Windows = storeRelease.Assets.First(a => a.Name.EndsWith("windows.7z")),
                    Linux = storeRelease.Assets.First(a => a.Name.EndsWith("ubuntu.7z")),
                    AppImage = storeRelease.Assets.First(a => a.Name.EndsWith("Vita3K-x86_64.AppImage")),
                    MacOS = storeRelease.Assets.First(a => a.Name.EndsWith("macos.dmg"))
                };
            }
            catch (Octokit.NotFoundException) {
                return new BuildAssets
                {
                    Windows = latestRelease.Assets.First(a => a.Name.StartsWith("windows-latest")),
                    Linux = latestRelease.Assets.First(a => a.Name.StartsWith("ubuntu-latest")),
                    AppImage = latestRelease.Assets.First(a => a.Name.StartsWith("Vita3K-x86_64.AppImage")),
                    MacOS = latestRelease.Assets.First(a => a.Name.StartsWith("macos-latest"))
                };
            }
        }
    }
}
