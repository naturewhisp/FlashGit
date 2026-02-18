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
        private readonly string _tempRemotePath;
        private readonly GitService _gitService;

        public GitServiceTests()
        {
            // Create a temporary directory for the repository
            _tempRepoPath = Path.Combine(Path.GetTempPath(), "TurboGitTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempRepoPath);

            // Create a temporary directory for the remote repository
            _tempRemotePath = Path.Combine(Path.GetTempPath(), "TurboGitTests_Remote_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempRemotePath);

            // Initialize Git repository
            Repository.Init(_tempRepoPath);
            Repository.Init(_tempRemotePath);

            _gitService = new GitService();
        }

        [Fact]
        public async Task FetchAsync_FetchesFromRemote()
        {
            // Arrange
            // 1. Create a commit in the remote repo
            using (var remoteRepo = new Repository(_tempRemotePath))
            {
                var signature = new Signature("Remote User", "remote@example.com", DateTimeOffset.Now);
                File.WriteAllText(Path.Combine(_tempRemotePath, "remote_file.txt"), "remote content");
                Commands.Stage(remoteRepo, "remote_file.txt");
                remoteRepo.Commit("Remote commit", signature, signature);
            }

            // 2. Add the remote to the local repo
            using (var localRepo = new Repository(_tempRepoPath))
            {
                localRepo.Network.Remotes.Add("origin", _tempRemotePath);
            }

            // Act
            await _gitService.FetchAsync(_tempRepoPath);

            // Assert
            using (var localRepo = new Repository(_tempRepoPath))
            {
                // Verify that the remote branch exists and has the commit
                var remoteBranch = localRepo.Branches["origin/master"];
                Assert.NotNull(remoteBranch);
                Assert.Equal("Remote commit", remoteBranch.Tip.MessageShort);
            }
        }

        [Fact]
        public async Task FetchAsync_WithSpecificRemote_FetchesFromThatRemote()
        {
            // Arrange
            // 1. Create a commit in the remote repo
            using (var remoteRepo = new Repository(_tempRemotePath))
            {
                var signature = new Signature("Remote User", "remote@example.com", DateTimeOffset.Now);
                File.WriteAllText(Path.Combine(_tempRemotePath, "remote_file_upstream.txt"), "remote content upstream");
                Commands.Stage(remoteRepo, "remote_file_upstream.txt");
                remoteRepo.Commit("Upstream commit", signature, signature);
            }

            // 2. Add the remote to the local repo with a custom name
            using (var localRepo = new Repository(_tempRepoPath))
            {
                localRepo.Network.Remotes.Add("upstream", _tempRemotePath);
            }

            // Act
            await _gitService.FetchAsync(_tempRepoPath, "upstream");

            // Assert
            using (var localRepo = new Repository(_tempRepoPath))
            {
                var remoteBranch = localRepo.Branches["upstream/master"];
                Assert.NotNull(remoteBranch);
                Assert.Equal("Upstream commit", remoteBranch.Tip.MessageShort);
            }
        }

        [Fact]
        public async Task FetchAsync_WithInvalidRemote_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _gitService.FetchAsync(_tempRepoPath, "invalid_remote"));
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
            CleanupDirectory(_tempRepoPath);
            CleanupDirectory(_tempRemotePath);
        }

        private void CleanupDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    // Helper to remove read-only attributes which git can set
                    SetAttributesNormal(path);
                    Directory.Delete(path, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public async Task GetFileStatusAsync_ExcludesIgnoredFiles()
        {
            // Arrange
            // Create a .gitignore file
            File.WriteAllText(Path.Combine(_tempRepoPath, ".gitignore"), "*.log");
            
            // Create an ignored file
            File.WriteAllText(Path.Combine(_tempRepoPath, "ignored.log"), "this should be ignored");
            
            // Create a normal file
            File.WriteAllText(Path.Combine(_tempRepoPath, "normal.txt"), "this should be seen");

            using (var repo = new Repository(_tempRepoPath))
            {
                Commands.Stage(repo, ".gitignore");
                // We don't stage the other files yet
            }

            // Act
            var statuses = await _gitService.GetFileStatusAsync(_tempRepoPath);
            var statusList = statuses.ToList();

            // Assert
            Assert.DoesNotContain(statusList, s => s.FilePath == "ignored.log");
            Assert.Contains(statusList, s => s.FilePath == "normal.txt");
            Assert.Contains(statusList, s => s.FilePath == ".gitignore");
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
