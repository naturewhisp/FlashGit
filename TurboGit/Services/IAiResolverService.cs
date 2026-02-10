using System.Threading.Tasks;

namespace TurboGit.Services
{
    /// <summary>
    /// Defines the contract for an AI service that can resolve merge conflicts.
    /// </summary>
    public interface IAiResolverService
    {
        /// <summary>
        /// The name of the AI provider (e.g., "Gemini Pro", "GitHub Copilot").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Attempts to resolve a merge conflict using an AI model.
        /// </summary>
        /// <param name="conflictedContent">The full text of the file with conflict markers.</param>
        /// <param name="languageHint">A hint about the programming language (e.g., "csharp", "python").</param>
        /// <returns>The resolved code block as a string. Throws exception if resolution fails.</returns>
        Task<string> ResolveConflictAsync(string conflictedContent, string languageHint);
    }
}
