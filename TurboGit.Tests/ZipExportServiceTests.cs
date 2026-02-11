using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using LibGit2Sharp;
using TurboGit.Services;
using Xunit;

namespace TurboGit.Tests
{
    public class ZipExportServiceTests : IDisposable
    {
        private readonly string _testRepoPath;
        private readonly string _testZipPath;
        private readonly string _testExtractPath;

        public ZipExportServiceTests()
        {
            _testRepoPath = Path.Combine(Path.GetTempPath(), "TurboGitTests_" + Guid.NewGuid());
            _testZipPath = Path.Combine(Path.GetTempPath(), "TurboGitTests_" + Guid.NewGuid() + ".zip");
            _testExtractPath = Path.Combine(Path.GetTempPath(), "TurboGitTests_Extract_" + Guid.NewGuid());

            Directory.CreateDirectory(_testRepoPath);
            Repository.Init(_testRepoPath);
        }

        public void Dispose()
        {
            Cleanup(_testRepoPath);
            Cleanup(_testExtractPath);
            if (File.Exists(_testZipPath))
            {
                try
                {
                    File.Delete(_testZipPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        private void Cleanup(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    // Recursive delete can fail if files are locked or read-only (git objects often are read-only)
                    SetAttributesNormal(new DirectoryInfo(path));
                    Directory.Delete(path, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        private void SetAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
                SetAttributesNormal(subDir);
            foreach (var file in dir.GetFiles())
                file.Attributes = FileAttributes.Normal;
        }

        [Fact]
        public async Task ExportCommitAsZip_ShouldCreateZipWithCorrectFiles()
        {
            // Arrange
            string rootFileName = "test.txt";
            string rootFileContent = "Hello World";
            string subDirName = "subdir";
            string subFileName = "nested.txt";
            string subFileContent = "Nested Content";

            // Use LibGit2Sharp to setup the repo
            using (var repo = new Repository(_testRepoPath))
            {
                // Create root file
                File.WriteAllText(Path.Combine(_testRepoPath, rootFileName), rootFileContent);
                Commands.Stage(repo, rootFileName);

                // Create subdirectory and file
                Directory.CreateDirectory(Path.Combine(_testRepoPath, subDirName));
                File.WriteAllText(Path.Combine(_testRepoPath, subDirName, subFileName), subFileContent);
                Commands.Stage(repo, Path.Combine(subDirName, subFileName));

                // Commit
                Signature author = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
                Commit commit = repo.Commit("Initial commit", author, author);

                var service = new ZipExportService();

                // Act
                await service.ExportCommitAsZipAsync(_testRepoPath, commit.Sha, _testZipPath);
            }

            // Assert
            Assert.True(File.Exists(_testZipPath), "Zip file was not created.");

            ZipFile.ExtractToDirectory(_testZipPath, _testExtractPath);

            string extractedRootFile = Path.Combine(_testExtractPath, rootFileName);
            string extractedSubFile = Path.Combine(_testExtractPath, subDirName, subFileName);

            Assert.True(File.Exists(extractedRootFile), "Root file missing in zip.");
            Assert.Equal(rootFileContent, File.ReadAllText(extractedRootFile));

            Assert.True(File.Exists(extractedSubFile), "Nested file missing in zip.");
            Assert.Equal(subFileContent, File.ReadAllText(extractedSubFile));

            // Verify .git folder is not exported
            Assert.False(Directory.Exists(Path.Combine(_testExtractPath, ".git")), ".git folder should not be in zip.");
        }
    }
}
