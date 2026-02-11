using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using TurboGit.Core.Models;
using TurboGit.Services;
using Xunit;

namespace TurboGit.Tests
{
    public class GitServiceTests : IDisposable
    {
        private readonly string _tempRepoPath;
        private readonly GitService _gitService;

        public GitServiceTests()
        {
            // Create a temporary directory for the repository
            _tempRepoPath = Path.Combine(Path.GetTempPath(), "TurboGitTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempRepoPath);

            // Initialize Git repository
            Repository.Init(_tempRepoPath);

            _gitService = new GitService();
        }

        [Fact]
        public async Task GetCommitHistoryAsync_ReturnsCorrectCommits()
        {
            // Arrange
            // Create commits in the repo
            using (var repo = new Repository(_tempRepoPath))
            {
                var baseTime = DateTimeOffset.Now;

                // Commit 1
                var signature1 = new Signature("Test User", "test@example.com", baseTime);
                File.WriteAllText(Path.Combine(_tempRepoPath, "file1.txt"), "content 1");
                Commands.Stage(repo, "file1.txt");
                repo.Commit("Initial commit", signature1, signature1);

                // Commit 2
                var signature2 = new Signature("Test User", "test@example.com", baseTime.AddSeconds(1));
                File.WriteAllText(Path.Combine(_tempRepoPath, "file2.txt"), "content 2");
                Commands.Stage(repo, "file2.txt");
                repo.Commit("Second commit", signature2, signature2);

                // Commit 3
                var signature3 = new Signature("Test User", "test@example.com", baseTime.AddSeconds(2));
                File.WriteAllText(Path.Combine(_tempRepoPath, "file1.txt"), "content 1 updated");
                Commands.Stage(repo, "file1.txt");
                repo.Commit("Third commit", signature3, signature3);
            }

            // Act
            var commits = await _gitService.GetCommitHistoryAsync(_tempRepoPath);
            var commitList = commits.ToList();

            // Assert
            Assert.NotNull(commitList);
            Assert.Equal(3, commitList.Count);

            // Verify order (most recent first)
            Assert.Equal("Third commit", commitList[0].Message);
            Assert.Equal("Test User", commitList[0].Author);

            Assert.Equal("Second commit", commitList[1].Message);

            Assert.Equal("Initial commit", commitList[2].Message);
        }

        public void Dispose()
        {
            // Cleanup
            if (Directory.Exists(_tempRepoPath))
            {
                try
                {
                    // Helper to remove read-only attributes which git can set
                    SetAttributesNormal(_tempRepoPath);
                    Directory.Delete(_tempRepoPath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        private void SetAttributesNormal(string dirPath)
        {
            foreach (string subDir in Directory.GetDirectories(dirPath))
            {
                SetAttributesNormal(subDir);
            }
            foreach (string file in Directory.GetFiles(dirPath))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }
    }
}
