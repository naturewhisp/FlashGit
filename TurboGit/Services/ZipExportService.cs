using System;
using System.IO.Compression;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace TurboGit.Services
{
    /// <summary>
    /// Service to handle exporting a commit as a Zip archive.
    /// </summary>
    public class ZipExportService : IZipExportService
    {
        /// <summary>
        /// Exports the state of the repository at a specific commit to a Zip file.
        /// The .git directory is excluded.
        /// implicit: This uses the commit tree, so it respects .gitignore (ignored files are not in the tree).
        /// </summary>
        /// <param name="repoPath">The repository root path.</param>
        /// <param name="commitSha">The SHA of the commit to export.</param>
        /// <param name="outputZipPath">The path where the Zip file will be saved.</param>
        public async Task ExportCommitAsZipAsync(string repoPath, string commitSha, string outputZipPath)
        {
            await Task.Run(() =>
            {
                using (var repo = new Repository(repoPath))
                {
                    var commit = repo.Lookup<Commit>(commitSha);
                    if (commit == null)
                    {
                        throw new ArgumentException($"Commit {commitSha} not found in repository {repoPath}.");
                    }

                    using (var zipArchive = ZipFile.Open(outputZipPath, ZipArchiveMode.Create))
                    {
                        AddTreeEntriesToZip(commit.Tree, zipArchive, "");
                    }
                }
            });
        }

        /// <summary>
        /// Recursively adds entries from a Git Tree to the Zip archive.
        /// </summary>
        /// <param name="tree">The Git tree to process.</param>
        /// <param name="archive">The Zip archive to add files to.</param>
        /// <param name="currentPath">The current path within the repository.</param>
        private void AddTreeEntriesToZip(Tree tree, ZipArchive archive, string currentPath)
        {
            foreach (var entry in tree)
            {
                var entryPath = string.IsNullOrEmpty(currentPath) ? entry.Name : $"{currentPath}/{entry.Name}";

                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    var blob = (Blob)entry.Target;
                    var entryInZip = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                    using (var entryStream = entryInZip.Open())
                    using (var blobStream = blob.GetContentStream())
                    {
                        blobStream.CopyTo(entryStream);
                    }
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    var subTree = (Tree)entry.Target;
                    AddTreeEntriesToZip(subTree, archive, entryPath);
                }
                // Symlinks and other types are ignored for simplicity.
            }
        }
    }
}
