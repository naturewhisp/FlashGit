// TurboGit/Services/AiResolverService.cs
using System.Threading.Tasks;

namespace TurboGit.Services
{
    /// <summary>
    /// Defines the contract for an AI service that can resolve merge conflicts.
    /// </summary>
    public interface IAiResolverService
    {
        /// <summary>
        /// Attempts to resolve a merge conflict using an AI model.
        /// </summary>
        /// <param name="conflictedContent">The full text of the file with conflict markers.</param>
        /// <param name="languageHint">A hint about the programming language (e.g., "csharp", "python").</param>
        /// <returns>The resolved code block as a string, or null if resolution fails.</returns>
        Task<string> ResolveConflictAsync(string conflictedContent, string languageHint);
    }

    /// <summary>
    /// Configuration settings for AI services.
    /// In a real app, this would be loaded from a secure settings file.
    /// </summary>
    public class AiServiceConfig
    {
        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
    }
}