// Modello che rappresenta un repository GitHub nell'applicazione.
namespace TurboGit.Models
{
    public class Repository
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CloneUrl { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public bool IsFork { get; set; }
    }
}
