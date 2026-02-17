using System;
using System.Text;
using System.Threading.Tasks;

namespace TurboGit.Services
{
    /// <summary>
    /// An implementation of IAiResolverService using GitHub Copilot's underlying model.
    /// Note: This is a conceptual implementation. A real one would use a specific Copilot API or library.
    /// </summary>
    public class GitHubCopilotResolver : IAiResolverService
    {
        private readonly AiServiceConfig _config;

        public string Name => "GitHub Copilot";

        public GitHubCopilotResolver(AiServiceConfig config)
        {
            _config = config;
            // Copilot auth is typically more complex, often using a GitHub token.
            // _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);
        }

        public async Task<string> ResolveConflictAsync(string conflictedContent, string languageHint)
        {
            if (string.IsNullOrEmpty(_config?.ApiKey))
            {
                throw new InvalidOperationException("GitHub Copilot API Key/Token is not configured.");
            }

            try
            {
                var prompt = BuildPrompt(conflictedContent, languageHint);

                Console.WriteLine($"--- Sending to GitHub Copilot (Simulated) ---\n{prompt}");

                // Simulate API call and potential errors
                await SimulateApiCallAsync();

                // Since we don't have a real endpoint, we return a simulated success response.
                return SimulateResponse(conflictedContent);
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("The request to GitHub Copilot timed out.");
            }
            catch (Exception ex)
            {
                // In a real implementation, we would catch specific HttpRequestExceptions
                throw new Exception($"GitHub Copilot error: {ex.Message}", ex);
            }
        }

        private async Task SimulateApiCallAsync()
        {
            // Simulate network latency
            await Task.Delay(500);

            // Simulate random error (optional, for testing robustness)
            // if (new Random().Next(0, 10) == 0) throw new HttpRequestException("Simulated network error");
        }

        private string BuildPrompt(string content, string language)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Task: Resolve the Git merge conflict in the following {language} code.");
            sb.AppendLine("Combine the changes to create a functional and correct code block.");
            sb.AppendLine("Provide only the final, resolved code without any conflict markers.");
            sb.AppendLine("--- CODE ---");
            sb.AppendLine(content);
            sb.AppendLine("--- RESOLVED CODE ---");
            return sb.ToString();
        }

        private string SimulateResponse(string originalContent)
        {
            // A simple simulation for demonstration.
            // It tries to find conflict markers and just pick the HEAD change + incoming change combined,
            // or just placeholders to show it worked.

            var sb = new StringBuilder();
            var lines = originalContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (line.StartsWith("<<<<<<<") || line.StartsWith("=======") || line.StartsWith(">>>>>>>"))
                {
                    continue; // Skip markers
                }
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
    }
}
