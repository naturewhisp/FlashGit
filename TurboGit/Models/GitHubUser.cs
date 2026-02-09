// Models/GitHubUser.cs
namespace TurboGit.Models
{
    /// <summary>
    /// Represents a GitHub user.
    /// Used to store user information after successful authentication.
    /// </summary>
    public class GitHubUser
    {
        public string Login { get; set; }
        public long Id { get; set; }
        public string AvatarUrl { get; set; }
        public string Name { get; set; }
    }
}
