using System.Collections.Generic;
using System.Threading.Tasks;
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
        public async Task AddRepository_ShouldAddRepo_WhenPathIsSelected()
        {
            // Arrange
            var mockRepoService = new Mock<IRepositoryService>();
            mockRepoService.Setup(s => s.GetRepositoriesAsync())
                .ReturnsAsync(new List<LocalRepository>());

            var viewModel = new MainWindowViewModel(mockRepoService.Object);

            // Mock the folder selection
            string selectedPath = "/new/repo/path";
            viewModel.RequestFolderSelection = () => Task.FromResult<string?>(selectedPath);

            // Act
            await viewModel.AddRepositoryCommand.ExecuteAsync(null);

            // Assert
            mockRepoService.Verify(s => s.AddRepositoryAsync(It.Is<LocalRepository>(r => r.Path == selectedPath)), Times.Once);
            Assert.Contains(viewModel.RepositoryList, r => r.Path == selectedPath);
            Assert.Equal(selectedPath, viewModel.SelectedRepository?.Path);
        }

        [Fact]
        public void OnSelectedRepositoryChanged_ShouldCallChildViewModels()
        {
            // Arrange
            var mockRepoService = new Mock<IRepositoryService>();
            mockRepoService.Setup(s => s.GetRepositoriesAsync())
                .ReturnsAsync(new List<LocalRepository>());

            var viewModel = new MainWindowViewModel(mockRepoService.Object);

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
