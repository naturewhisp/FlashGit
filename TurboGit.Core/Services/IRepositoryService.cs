using System.Collections.Generic;
using TurboGit.Core.Models;

namespace TurboGit.Core.Services;

/// <summary>
/// Defines the contract for a service that interacts with Git repositories.
/// </summary>
public interface IRepositoryService
{
    /// <summary>
    /// Opens a Git repository at the specified path.
    /// </summary>
    /// <param name="path">The file path to the repository.</param>
    /// <returns>A RepositoryInfo object.</returns>
    RepositoryInfo OpenRepository(string path);

    /// <summary>
    /// Gets the status of the repository, including changed files.
    /// </summary>
    /// <param name="repoPath">The path to the repository.</param>
    /// <returns>A collection of strings representing file status.</returns>
    IEnumerable<string> GetRepositoryStatus(string repoPath);
    
    /// <summary>
    /// Fetches the latest changes from the remote.
    /// </summary>
    /// <param name="repoPath">The path to the repository.</param>
    /// <param name="remoteName">The name of the remote to fetch from. Defaults to "origin".</param>
    void Fetch(string repoPath, string remoteName = "origin");
}
