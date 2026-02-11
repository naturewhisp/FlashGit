using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TurboGit.Services;
using TurboGit.ViewModels;
using Xunit;
using Xunit.Abstractions;
using System.Collections.ObjectModel;

namespace TurboGit.Tests
{
    public class HistoryViewModelPerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public HistoryViewModelPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task LoadCommits_Performance_Baseline()
        {
            // Arrange
            int commitCount = 5000;
            var commits = Enumerable.Range(0, commitCount).Select(i => new GitCommit
            {
                Sha = $"sha{i}",
                Message = $"Commit message {i}",
                Author = "Author",
                CommitDate = System.DateTimeOffset.Now
            }).ToList();

            var mockGitService = new Mock<IGitService>();
            mockGitService.Setup(s => s.GetCommitHistoryAsync(It.IsAny<string>(), It.IsAny<int>()))
                          .ReturnsAsync(commits);

            var viewModel = new HistoryViewModel(mockGitService.Object);

            int eventCount = 0;
            viewModel.Commits.CollectionChanged += (s, e) =>
            {
                eventCount++;
                // Simulate slight UI overhead
                Thread.SpinWait(100);
            };

            // Act
            var stopwatch = Stopwatch.StartNew();
            await viewModel.LoadCommits("dummy/path");
            stopwatch.Stop();

            // Assert
            Assert.Equal(commitCount, viewModel.Commits.Count);
            _output.WriteLine($"LoadCommits took {stopwatch.ElapsedMilliseconds} ms for {commitCount} items. Events fired: {eventCount}");
        }
    }
}
