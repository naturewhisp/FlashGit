using System.Threading.Tasks;

namespace TurboGit.Services.AI
{
    /// <summary>
    /// Defines the contract for an AI-based merge conflict resolver.
    /// </summary>
    public interface IAiResolverService
    {
        /// <summary>
        /// The name of the AI provider (e.g., "Gemini Pro", "GitHub Copilot").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Resolves a merge conflict using the AI model.
        /// </summary>
        /// <param name="apiKey">The API key for the service.</param>
        /// <param name="currentChange">The code block for the current (HEAD) change.</param>
        /// <param name="incomingChange">The code block for the incoming change.</param>
        /// <param name="language">The programming language of the code (e.g., "csharp").</param>
        /// <returns>The resolved code block as suggested by the AI.</returns>
        Task<string> ResolveConflict(string apiKey, string currentChange, string incomingChange, string language);
    }
}
