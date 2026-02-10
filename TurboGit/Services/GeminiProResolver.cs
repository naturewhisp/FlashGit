using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TurboGit.Services
{
    /// <summary>
    /// An implementation of IAiResolverService using the Google Gemini Pro model.
    /// </summary>
    public class GeminiProResolver : IAiResolverService
    {
        private readonly AiServiceConfig _config;
        private readonly HttpClient _httpClient;

        public string Name => "Gemini Pro";

        public GeminiProResolver(AiServiceConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set timeout
        }

        public async Task<string> ResolveConflictAsync(string conflictedContent, string languageHint)
        {
            if (string.IsNullOrEmpty(_config?.ApiKey))
            {
                throw new InvalidOperationException("Gemini Pro API Key is not configured.");
            }

            try
            {
                // 1. Construct the prompt for the LLM.
                var prompt = BuildPrompt(conflictedContent, languageHint);

                // 2. Prepare the request
                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                var jsonBody = JsonSerializer.Serialize(requestBody);
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_config.ApiKey}";

                // 3. Make the API call
                var response = await _httpClient.PostAsync(endpoint, new StringContent(jsonBody, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Gemini Pro API request failed with status code {response.StatusCode}: {errorContent}");
                }

                // 4. Parse the response
                var responseString = await response.Content.ReadAsStringAsync();
                var result = ParseGeminiResponse(responseString);

                if (string.IsNullOrWhiteSpace(result))
                {
                     throw new InvalidOperationException("Gemini Pro returned an empty response.");
                }

                return ExtractCodeBlock(result);
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("The request to Gemini Pro timed out.");
            }
            catch (Exception ex) when (ex is not HttpRequestException && ex is not TimeoutException && ex is not InvalidOperationException)
            {
                 throw new Exception($"An unexpected error occurred while resolving conflict: {ex.Message}", ex);
            }
        }

        private string BuildPrompt(string content, string language)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"You are an expert programmer specializing in resolving Git merge conflicts in {language}.");
            sb.AppendLine("The following code block contains merge conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`).");
            sb.AppendLine("Analyze the logic of both the 'current change' (HEAD) and the 'incoming change'.");
            sb.AppendLine("Your task is to merge them into a single, correct, and syntactically valid code block.");
            sb.AppendLine("IMPORTANT: Return ONLY the resolved code. Do NOT include any markdown formatting (like ```csharp ... ```) or explanations.");
            sb.AppendLine("If the code is surrounded by conflict markers, remove them and merge the content intelligently.");
            sb.AppendLine("\n--- CONFLICTED CODE ---");
            sb.AppendLine(content);
            sb.AppendLine("\n--- END OF CODE ---");
            return sb.ToString();
        }

        private string? ParseGeminiResponse(string jsonResponse)
        {
            try
            {
                using (var doc = JsonDocument.Parse(jsonResponse))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        var firstCandidate = candidates[0];
                        if (firstCandidate.TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0)
                        {
                            return parts[0].GetProperty("text").GetString();
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Fallback or log error
            }
            return null;
        }

        private string ExtractCodeBlock(string? text)
        {
             // Remove potential markdown code blocks if the model ignores the instruction
             if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

             var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
             var sb = new StringBuilder();
             bool insideCodeBlock = false;
             bool foundCodeBlock = false;

             foreach(var line in lines)
             {
                 if (line.Trim().StartsWith("```"))
                 {
                     if (insideCodeBlock)
                     {
                         insideCodeBlock = false; // End of block
                     }
                     else
                     {
                         insideCodeBlock = true; // Start of block
                         foundCodeBlock = true;
                     }
                     continue;
                 }

                 if (insideCodeBlock || !foundCodeBlock)
                 {
                     // If we found a code block, we only append lines inside it.
                     // If we haven't found any code block yet, we assume the whole text is code (unless it starts later).
                     // This is a simple heuristic. Better to prompt effectively.
                     if (foundCodeBlock && !insideCodeBlock) continue;

                     sb.AppendLine(line);
                 }
             }

             var result = sb.ToString().Trim();
             // If heuristic failed (e.g. no markdown), return original text cleaned up
             if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(text)) return text.Trim();

             return result;
        }
    }
}
