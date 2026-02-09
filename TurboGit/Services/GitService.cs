// TurboGit/Services/GitService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using TurboGit.ViewModels;

namespace TurboGit.Services
{
    public interface IGitService
    {
        Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string repoPath, int limit = 100);
        Task<IEnumerable<GitFileStatus>> GetFileStatusAsync(string repoPath);
        Task StageFileAsync(string repoPath, string filePath);
        Task UnstageFileAsync(string repoPath, string filePath);
        Task<string> GetFileDiffAsync(string repoPath, string filePath, bool staged);
    }

    public class GitService : IGitService
    {
        // ... (GetCommitHistoryAsync remains the same)
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
                    Console.WriteLine($"Error getting commit history: {ex.Message}");
                    return Enumerable.Empty<GitCommit>();
                }
            });
        }
        
        public Task<IEnumerable<GitFileStatus>> GetFileStatusAsync(string repoPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var repo = new Repository(repoPath))
                    {
                        var statuses = new List<GitFileStatus>();
                        // This provides a comprehensive status of all files.
                        foreach (var item in repo.RetrieveStatus(new StatusOptions()))
                        {
                             bool isStaged = item.State.HasFlag(FileStatus.Staged) || item.State.HasFlag(FileStatus.Added);
                             
                             // We show the file in the appropriate list.
                             // A file can be both staged and modified in workdir, but we simplify for now.
                             if(isStaged)
                             {
                                statuses.Add(new GitFileStatus { FilePath = item.FilePath, IsStaged = true, Status = item.State.ToString() });
                             }
                             else // Includes workdir changes
                             {
                                statuses.Add(new GitFileStatus { FilePath = item.FilePath, IsStaged = false, Status = item.State.ToString() });
                             }
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

        public Task StageFileAsync(string repoPath, string filePath)
        {
            return Task.Run(() =>
            {
                using (var repo = new Repository(repoPath))
                {
                    Commands.Stage(repo, filePath);
                }
            });
        }

        public Task UnstageFileAsync(string repoPath, string filePath)
        {
            return Task.Run(() =>
            {
                using (var repo = new Repository(repoPath))
                {
                    // Unstage is more complex; it means resetting the change from the index to HEAD.
                    Commands.Unstage(repo, filePath);
                }
            });
        }

        public Task<string> GetFileDiffAsync(string repoPath, string filePath, bool staged)
        {
            return Task.Run(() =>
            {
                using (var repo = new Repository(repoPath))
                {
                    var compareOptions = new CompareOptions { ContextLines = 3, InterhunkLines = 1 };
                    Patch patch;

                    if (staged)
                    {
                        // Diff between the index (staged) and the HEAD commit
                        var headCommit = repo.Head.Tip;
                        patch = repo.Diff.Compare<Patch>(headCommit?.Tree, repo.Index, new[] { filePath }, null, compareOptions);
                    }
                    else
                    {
                        // Diff between the working directory and the index
                        patch = repo.Diff.Compare<Patch>(repo.Index, DiffTargets.WorkingDirectory, new[] { filePath }, null, compareOptions);
                    }

                    return patch?.Content ?? "No changes.";
                }
            });
        }
    }
}