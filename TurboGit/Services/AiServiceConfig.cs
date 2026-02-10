namespace TurboGit.Services
{
    /// <summary>
    /// Configuration settings for AI services.
    /// In a real app, this would be loaded from a secure settings file.
    /// </summary>
    public class AiServiceConfig
    {
        public required string ApiKey { get; set; }
        public string? Endpoint { get; set; }
    }
}
