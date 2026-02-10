using LibGit2Sharp;

namespace TurboGit.Core;

/// <summary>
/// A service class to encapsulate all Git operations using LibG it2Sharp.
/// This acts as a wrapper around the library to provide a clean interface
/// for the rest of the application.
/// </summary>
public class GitService
{
    /// <summary>
    /// Checks if the given path points to a valid Git repository.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>True if it's a valid repository, false otherwise.</returns>
    public bool IsValidRepository(string path)
    {
        return Repository.IsValid(path);
    }

    /// <summary>
    /// Opens a Git repository at the specified path.
    /// </summary>
    /// <param name="path">The path to the repository.</param>
    /// <returns>A LibGit2Sharp Repository object.</returns>
    /// <exception cref="RepositoryNotFoundException">Thrown if the path does not contain a valid repository.</exception>
    public Repository OpenRepository(string path)
    {
        try
        {
            return new Repository(path);
        }
        catch (RepositoryNotFoundException ex)
        {
            // Log the exception or handle it as needed.
            // For now, re-throwing to let the caller handle it.
            Console.WriteLine($"Error opening repository at {path}: {ex.Message}");
            throw;
        }
    }
}