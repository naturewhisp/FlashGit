// TurboGit/Services/GeminiProResolver.cs
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
// using System.Text.Json; // Would be used for actual JSON serialization

namespace TurboGit.Services
{
    /// <summary>
    /// An implementation of IAiResolverService using the Google Gemini Pro model.
    /// </summary>
    public class GeminiProResolver : IAiResolverService
    {
        private readonly AiServiceConfig _config;
        private readonly HttpClient _httpClient;

        public GeminiProResolver(AiServiceConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            // In a real app, you would configure HttpClient with Authorization headers.
            // _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        }

        public async Task<string> ResolveConflictAsync(string conflictedContent, string languageHint)
        {
            if (string.IsNullOrEmpty(_config?.ApiKey))
            {
                return "Error: Gemini Pro API Key is not configured.";
            }

            // 1. Construct the prompt for the LLM.
            var prompt = BuildPrompt(conflictedContent, languageHint);

            // 2. Simulate the API call.
            Console.WriteLine($"--- Sending to Gemini Pro ---\n{prompt}");

            // In a real implementation, you would make an HTTP POST request:
            // var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            // var jsonBody = JsonSerializer.Serialize(requestBody);
            // var response = await _httpClient.PostAsync(_config.Endpoint, new StringContent(jsonBody, Encoding.UTF8, "application/json"));
            // if(response.IsSuccessStatusCode) { ... parse response ... }

            // 3. Return a mocked response for demonstration purposes.
            await Task.Delay(500); // Simulate network latency.
            return SimulateResponse(conflictedContent);
        }

        private string BuildPrompt(string content, string language)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"You are an expert programmer specializing in resolving Git merge conflicts in {language}.");
            sb.AppendLine("The following code block contains merge conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`).");
            sb.AppendLine("Analyze the logic of both the 'current change' and the 'incoming change'.");
            sb.AppendLine("Your task is to merge them into a single, correct, and syntactically valid code block.");
            sb.AppendLine("Do NOT include the conflict markers in your response. Only provide the final, resolved code.");
            sb.AppendLine("\n--- CONFLICTED CODE ---");
            sb.AppendLine(content);
            sb.AppendLine("\n--- END OF CODE ---");
            sb.AppendLine("\nPlease provide the resolved code:");
            return sb.ToString();
        }

        private string SimulateResponse(string originalContent)
        {
            // A simple simulation that just removes the conflict markers and combines the code.
            // A real AI would perform a much more intelligent merge.
            var lines = originalContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var sb = new StringBuilder();
            bool inConflict = false;
            foreach(var line in lines)
            {
                if (line.StartsWith("<<<<<<<") || line.StartsWith("======="))
                {
                    inConflict = true;
                    continue;
                }
                if (line.StartsWith(">>>>>>>"))
                {
                    inConflict = false;
                    continue;
                }
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
    }
}