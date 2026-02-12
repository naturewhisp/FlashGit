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
    public class GitStatusTests : IDisposable
    {
        private readonly string _tempRepoPath;
        private readonly GitService _gitService;

        public GitStatusTests()
        {
            _tempRepoPath = Path.Combine(Path.GetTempPath(), "TurboGitStatusTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempRepoPath);
            Repository.Init(_tempRepoPath);
            _gitService = new GitService();
        }

        [Fact]
        public async Task GetFileStatusAsync_ReturnsStructuredStatus()
        {
            using (var repo = new Repository(_tempRepoPath))
            {
                // 1. Create a new file (Untracked)
                var file1 = Path.Combine(_tempRepoPath, "file1.txt");
                File.WriteAllText(file1, "content 1");

                // Check status: Should be NewInWorkdir
                var statuses = await _gitService.GetFileStatusAsync(_tempRepoPath);
                var status1 = statuses.FirstOrDefault(s => s.FilePath == "file1.txt");
                Assert.NotNull(status1);
                Assert.Equal(CoreFileStatus.NewInWorkdir, status1.Status);
                Assert.False(status1.IsStaged);

                // 2. Stage the file
                Commands.Stage(repo, "file1.txt");

                // Check status: Should be NewInIndex
                statuses = await _gitService.GetFileStatusAsync(_tempRepoPath);
                status1 = statuses.FirstOrDefault(s => s.FilePath == "file1.txt");
                Assert.NotNull(status1);
                Assert.Equal(CoreFileStatus.NewInIndex, status1.Status);
                Assert.True(status1.IsStaged);

                // 3. Commit the file
                var signature = new Signature("Test", "test@test.com", DateTimeOffset.Now);
                repo.Commit("Initial commit", signature, signature);

                // Check status: Should be empty (Clean)
                statuses = await _gitService.GetFileStatusAsync(_tempRepoPath);
                Assert.Empty(statuses);

                // 4. Modify the file (ModifiedInWorkdir)
                File.WriteAllText(file1, "content 1 modified");

                // Check status
                statuses = await _gitService.GetFileStatusAsync(_tempRepoPath);
                status1 = statuses.FirstOrDefault(s => s.FilePath == "file1.txt");
                Assert.NotNull(status1);
                Assert.Equal(CoreFileStatus.ModifiedInWorkdir, status1.Status);
                Assert.False(status1.IsStaged);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRepoPath))
            {
                try
                {
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
