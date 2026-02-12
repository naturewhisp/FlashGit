namespace TurboGit.Core.Models
{
    public class GitFileStatus
    {
        public string FilePath { get; set; } = string.Empty;
        public bool IsStaged { get; set; }
        public CoreFileStatus Status { get; set; }
    }
}
