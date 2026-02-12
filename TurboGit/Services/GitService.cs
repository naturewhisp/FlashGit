// TurboGit/Services/GitService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using TurboGit.Core.Models;
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
        public Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string repoPath, int limit = 100)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var repo = new Repository(repoPath))
                    {
                        return (IEnumerable<GitCommit>)repo.Commits.Take(limit).Select(c => new GitCommit
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
                        foreach (var item in repo.RetrieveStatus(new StatusOptions()))
                        {
                             bool isStaged = item.State.HasFlag(FileStatus.NewInIndex) ||
                                             item.State.HasFlag(FileStatus.ModifiedInIndex) ||
                                             item.State.HasFlag(FileStatus.DeletedFromIndex) ||
                                             item.State.HasFlag(FileStatus.RenamedInIndex) ||
                                             item.State.HasFlag(FileStatus.TypeChangeInIndex);

                             if(isStaged)
                             {
                                statuses.Add(new GitFileStatus { FilePath = item.FilePath, IsStaged = true, Status = (CoreFileStatus)item.State });
                             }
                             else
                             {
                                statuses.Add(new GitFileStatus { FilePath = item.FilePath, IsStaged = false, Status = (CoreFileStatus)item.State });
                             }
                        }
                        return (IEnumerable<GitFileStatus>)statuses;
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
                        // Staged changes: Diff between HEAD Tree and Index
                        var headCommit = repo.Head.Tip;
                        if (headCommit != null)
                        {
                             patch = repo.Diff.Compare<Patch>(headCommit.Tree, DiffTargets.Index, new[] { filePath }, null, compareOptions);
                        }
                        else
                        {
                             // Initial commit scenario (no HEAD), compare empty tree (or just use index for new files)
                             // For simplicity, handle new files in empty repo if needed, but usually HEAD exists or we compare against empty.
                             // LibGit2Sharp might throw if Head is null.
                             patch = null; // Fallback
                        }
                    }
                    else
                    {
                        // Unstaged changes: Diff between Index and Working Directory
                        patch = repo.Diff.Compare<Patch>(new[] { filePath }, true, null, compareOptions);
                        // Note: overload might be (IEnumerable<string>, bool includeUntracked, ...)
                        // checking overload: Compare<Patch>(IEnumerable<string> paths = null, bool includeUntracked = false, ...)
                    }

                    return patch?.Content ?? "No changes.";
                }
            });
        }
    }
}
