using System.Threading.Tasks;

namespace TurboGit.Services
{
    public interface IZipExportService
    {
        /// <summary>
        /// Exports the repository content at a specific commit to a zip file.
        /// </summary>
        /// <param name="repoPath">Path to the repository root.</param>
        /// <param name="commitSha">SHA hash of the commit to export.</param>
        /// <param name="outputZipPath">Full path for the output zip file.</param>
        Task ExportCommitAsZipAsync(string repoPath, string commitSha, string outputZipPath);
    }
}
