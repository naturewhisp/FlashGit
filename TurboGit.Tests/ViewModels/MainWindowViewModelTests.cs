using Moq;
using TurboGit.Core.Models;
using TurboGit.Services;
using TurboGit.ViewModels;
using Xunit;

namespace TurboGit.Tests.ViewModels
{
    public class MainWindowViewModelTests
    {
        [Fact]
        public void OnSelectedRepositoryChanged_ShouldCallChildViewModels()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            // Pass nulls for optional arguments to satisfy Moq's constructor resolution
            var mockHistoryViewModel = new Mock<HistoryViewModel>((IGitService?)null, (IZipExportService?)null);
            var mockStagingViewModel = new Mock<StagingViewModel>();

            // Inject mocks
            viewModel.HistoryViewModel = mockHistoryViewModel.Object;
            viewModel.StagingViewModel = mockStagingViewModel.Object;

            var repository = new LocalRepository
            {
                Name = "TestRepo",
                Path = "/path/to/repo"
            };

            // Act
            viewModel.SelectedRepository = repository;

            // Assert
            mockHistoryViewModel.Verify(vm => vm.LoadCommits(repository.Path), Times.Once);
            mockStagingViewModel.Verify(vm => vm.LoadChanges(repository.Path), Times.Once);
        }
    }
}
