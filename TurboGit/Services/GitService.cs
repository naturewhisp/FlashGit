// TurboGit/Services/GitService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp; // The core Git library
using TurboGit.ViewModels; // To use the GitCommit and GitFileStatus models

namespace TurboGit.Services
{
    /// <summary>
    /// Defines the contract for a service that interacts with a Git repository.
    /// All methods are asynchronous to ensure a non-blocking UI.
    /// </summary>
    public interface IGitService
    {
        Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string repoPath, int limit = 100);
        Task<IEnumerable<GitFileStatus>> GetFileStatusAsync(string repoPath);
    }

    /// <summary>
    /// A placeholder implementation of IGitService.
    /// This service wraps LibGit2Sharp operations to be consumed by ViewModels.
    /// In a real application, this would contain robust error handling and be registered for DI.
    /// </summary>
    public class GitService : IGitService
    {
        /// <summary>
        /// Asynchronously retrieves the commit history.
        /// Note: LibGit2Sharp is synchronous, so we wrap calls in Task.Run to offload from the UI thread.
        /// </summary>
        public Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string repoPath, int limit = 100)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var repo = new Repository(repoPath))
                    {
                        return repo.Commits.Take(limit).Select(c => new GitCommit
                        {
                            Sha = c.Sha,
                            Message = c.MessageShort,
                            Author = c.Author.Name,
                            CommitDate = c.Author.When
                        }).ToList();
                    }
                }
                catch (Exception ex)
                {
                    // Basic error handling. A real app should log this properly.
                    Console.WriteLine($"Error getting commit history: {ex.Message}");
                    return Enumerable.Empty<GitCommit>();
                }
            });
        }

        /// <summary>
        /// Asynchronously retrieves the status of all files in the working directory and index.
        /// </summary>
        public Task<IEnumerable<GitFileStatus>> GetFileStatusAsync(string repoPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var repo = new Repository(repoPath))
                    {
                        var statuses = new List<GitFileStatus>();
                        var repoStatus = repo.RetrieveStatus();

                        // Unstaged changes (working directory)
                        var unstaged = repoStatus.Modified.Concat(repoStatus.Untracked).Concat(repoStatus.Missing);
                        foreach(var item in unstaged)
                        {
                            statuses.Add(new GitFileStatus { FilePath = item.FilePath, IsStaged = false, Status = item.State.ToString() });
                        }

                        // Staged changes (index)
                        var staged = repoStatus.Staged.Concat(repoStatus.Added);
                         foreach(var item in staged)
                        {
                            statuses.Add(new GitFileStatus { FilePath = item.FilePath, IsStaged = true, Status = item.State.ToString() });
                        }

                        return statuses;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting file status: {ex.Message}");
                    return Enumerable.Empty<GitFileStatus>();
                }
            });
        }
    }
}
