// Models/GitHubUser.cs
namespace TurboGit.Models
{
    /// <summary>
    /// Represents a GitHub user.
    /// Used to store user information after successful authentication.
    /// </summary>
    public class GitHubUser
    {
        public required string Login { get; set; }
        public long Id { get; set; }
        public required string AvatarUrl { get; set; }
        public required string Name { get; set; }
    }
}
