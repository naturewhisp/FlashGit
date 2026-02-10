// Modello che rappresenta il profilo utente di GitHub.
namespace TurboGit.Models
{
    public class UserProfile
    {
        public string Login { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
