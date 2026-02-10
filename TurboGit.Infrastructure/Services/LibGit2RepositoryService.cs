using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TurboGit.Core.Models;
using TurboGit.Core.Services;

namespace TurboGit.Infrastructure.Services;

/// <summary>
/// Service for interacting with Git repositories using LibGit2Sharp.
/// </summary>
public class LibGit2RepositoryService : IRepositoryService
{
    /// <summary>
    /// Opens a Git repository and returns its basic information.
    /// </summary>
    /// <param name="path">The path to the repository.</param>
    /// <returns>A RepositoryInfo object.</returns>
    public RepositoryInfo OpenRepository(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        if (!Repository.IsValid(path))
        {
            throw new ArgumentException("The specified path is not a valid Git repository.", nameof(path));
        }

        using (var repo = new Repository(path))
        {
            return new RepositoryInfo
            {
                Name = new DirectoryInfo(repo.Info.WorkingDirectory).Name,
                Path = repo.Info.WorkingDirectory
            };
        }
    }

    /// <summary>
    /// Gets the status of the repository, returning a list of modified or new files.
    /// </summary>
    /// <param name="repoPath">The path to the repository.</param>
    /// <returns>A list of file statuses.</returns>
    public IEnumerable<string> GetRepositoryStatus(string repoPath)
    {
        using (var repo = new Repository(repoPath))
        {
            var status = repo.RetrieveStatus(new StatusOptions());
            return status
                .Where(s => s.State != FileStatus.Ignored)
                .Select(s => $"{s.FilePath}: {s.State}")
                .ToList();
        }
    }

    /// <summary>
    /// Fetches from the 'origin' remote.
    /// Note: This is a simplified fetch and does not include credentials.
    /// </summary>
    /// <param name="repoPath">The path to the repository.</param>
    public void Fetch(string repoPath)
    {
        using (var repo = new Repository(repoPath))
        {
            var remote = repo.Network.Remotes["origin"];
            if (remote != null)
            {
                // For simplicity, this example doesn't handle credentials.
                // A real implementation would require a CredentialsProvider.
                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(repo, remote.Name, refSpecs, null, "Fetching from origin");
            }
        }
    }
}
