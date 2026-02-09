namespace TurboGit.Core.Models;

/// <summary>
/// Represents basic information about a Git repository.
/// </summary>
public class RepositoryInfo
{
    public required string Name { get; set; }
    public required string Path { get; set; }
}
