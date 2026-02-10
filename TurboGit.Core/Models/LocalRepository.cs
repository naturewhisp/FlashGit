namespace TurboGit.Core.Models;

/// <summary>
    /// Represents a Git repository managed by the application.
    /// This is a simple data model for UI sdisplay purposes.
    /// </summary>
public class LocalRepository
{
    /// <summary>
    /// The friendly name of the repository (e.g., the folder name).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The full, absolute path to the repository's working directory.
    /// </summary>
    public required string Path { get; set; }
}