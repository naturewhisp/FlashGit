using System.ComponentModel;
using System.IO.Compression;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace TurboGit.Services
{
    /// <summary>
    /// Service to handle exporting a commit as a Zip archive.
    /// </summary>
    public class ZipExportService
    {
        /// <summary>
        /// Exports the state of the repository at a specific commit to a Zip file.
        /// The .git directory is excluded.
        /// </summary>
        /// <param name="commit">The commit to export.</param>
        /// <param name="outputZipPath">The path where the Zip file will be saved.</param>
        public async Task ExportCommitAsZip(Commit commit, string outputZipPath)
        {
            await Task.Run(() =>
            {
                var tree = commit.Tree;
                using (var zipArchive = ZipFile.Open(outputZipPath, ZipArchiveMode.Create))
                {
                    AddTreeEntriesToZip(tree, zipArchive, "");
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
