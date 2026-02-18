// TurboGit/Services/GitService.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;
using TurboGit.Core.Models;
using TurboGit.Infrastructure.Security;
using TurboGit.ViewModels;

namespace TurboGit.Services
{
    public interface IGitService
    {
        Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string repoPath, int limit = 100);
        Task<IEnumerable<GitFileStatus>> GetFileStatusAsync(string repoPath);
        Task StageFileAsync(string repoPath, string filePath);
        Task UnstageFileAsync(string repoPath, string filePath);
        Task FetchAsync(string repoPath, string? remoteName = null);
        Task<string> GetFileDiffAsync(string repoPath, string filePath, bool staged);
        Task<DiffModel> GetFileDiffModelAsync(string repoPath, string filePath, bool staged);
        Task StageLinesAsync(string repoPath, string filePath, IEnumerable<DiffLine> selectedLines, IEnumerable<DiffHunk> allHunks);
        Task UnstageLinesAsync(string repoPath, string filePath, IEnumerable<DiffLine> selectedLines, IEnumerable<DiffHunk> allHunks);
    }

    public class GitService : IGitService
    {
        public const string DefaultRemoteName = "origin";
        private readonly ITokenManager _tokenManager;

        public GitService() : this(new TokenManager())
        {
        }

        public GitService(ITokenManager tokenManager)
        {
            _tokenManager = tokenManager;
        }

        public Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string repoPath, int limit = 100)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var repo = new Repository(repoPath))
                    {
                        return (IEnumerable<GitCommit>)repo.Commits.Take(limit).Select(c => new GitCommit
                        {
                            Sha = c.Sha,
                            Message = c.MessageShort,
                            Author = c.Author.Name,
                            CommitDate = c.Author.When
                        }).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting commit history: {ex.Message}");
                    return Enumerable.Empty<GitCommit>();
                }
            });
        }

        public Task<IEnumerable<GitFileStatus>> GetFileStatusAsync(string repoPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var repo = new Repository(repoPath))
                    {
                        var statuses = new List<GitFileStatus>();
                        foreach (var item in repo.RetrieveStatus(new StatusOptions()))
                        {
                             if (item.State.HasFlag(FileStatus.Ignored))
                             {
                                 continue;
                             }

                             bool isStaged = item.State.HasFlag(FileStatus.NewInIndex) ||
                                             item.State.HasFlag(FileStatus.ModifiedInIndex) ||
                                             item.State.HasFlag(FileStatus.DeletedFromIndex) ||
                                             item.State.HasFlag(FileStatus.RenamedInIndex) ||
                                             item.State.HasFlag(FileStatus.TypeChangeInIndex);

                             bool isUnstaged = item.State.HasFlag(FileStatus.NewInWorkdir) ||
                                              item.State.HasFlag(FileStatus.ModifiedInWorkdir) ||
                                              item.State.HasFlag(FileStatus.DeletedFromWorkdir) ||
                                              item.State.HasFlag(FileStatus.RenamedInWorkdir) ||
                                              item.State.HasFlag(FileStatus.TypeChangeInWorkdir);

                             // A file can appear in BOTH lists after partial staging
                             if (isStaged)
                             {
                                statuses.Add(new GitFileStatus { FilePath = item.FilePath, IsStaged = true, Status = (CoreFileStatus)item.State });
                             }
                             if (isUnstaged)
                             {
                                statuses.Add(new GitFileStatus { FilePath = item.FilePath, IsStaged = false, Status = (CoreFileStatus)item.State });
                             }
                             // Fallback: if neither flag matched, add as unstaged
                             if (!isStaged && !isUnstaged)
                             {
                                statuses.Add(new GitFileStatus { FilePath = item.FilePath, IsStaged = false, Status = (CoreFileStatus)item.State });
                             }
                        }
                        return (IEnumerable<GitFileStatus>)statuses;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting file status: {ex.Message}");
                    return Enumerable.Empty<GitFileStatus>();
                }
            });
        }

        public Task StageFileAsync(string repoPath, string filePath)
        {
            return Task.Run(() =>
            {
                using (var repo = new Repository(repoPath))
                {
                    Commands.Stage(repo, filePath);
                }
            });
        }

        public Task UnstageFileAsync(string repoPath, string filePath)
        {
            return Task.Run(() =>
            {
                using (var repo = new Repository(repoPath))
                {
                    Commands.Unstage(repo, filePath);
                }
            });
        }

        public Task<string> GetFileDiffAsync(string repoPath, string filePath, bool staged)
        {
            return Task.Run(() =>
            {
                using (var repo = new Repository(repoPath))
                {
                    var compareOptions = new CompareOptions { ContextLines = 3, InterhunkLines = 1 };
                    Patch patch;

                    if (staged)
                    {
                        // Staged changes: Diff between HEAD Tree and Index
                        var headCommit = repo.Head.Tip;
                        if (headCommit != null)
                        {
                             patch = repo.Diff.Compare<Patch>(headCommit.Tree, DiffTargets.Index, new[] { filePath }, null, compareOptions);
                        }
                        else
                        {
                             // Initial commit scenario (no HEAD), compare empty tree (or just use index for new files)
                             // For simplicity, handle new files in empty repo if needed, but usually HEAD exists or we compare against empty.
                             // LibGit2Sharp might throw if Head is null.
                             patch = null; // Fallback
                        }
                    }
                    else
                    {
                        // Unstaged changes: Diff between Index and Working Directory
                        patch = repo.Diff.Compare<Patch>(new[] { filePath }, true, null, compareOptions);
                        // Note: overload might be (IEnumerable<string>, bool includeUntracked, ...)
                        // checking overload: Compare<Patch>(IEnumerable<string> paths = null, bool includeUntracked = false, ...)
                    }

                    return patch?.Content ?? "No changes.";
                }
            });
        }

        public Task FetchAsync(string repoPath, string? remoteName = null)
        {
            return Task.Run(() =>
            {
                using (var repo = new Repository(repoPath))
                {
                    Remote remote;
                    if (!string.IsNullOrEmpty(remoteName))
                    {
                        remote = repo.Network.Remotes[remoteName];
                        if (remote == null)
                        {
                            throw new ArgumentException($"Remote '{remoteName}' not found in repository.", nameof(remoteName));
                        }
                    }
                    else
                    {
                        remote = repo.Network.Remotes[DefaultRemoteName];
                        if (remote == null)
                        {
                            // Try fallback to the first remote if default remote doesn't exist
                            remote = repo.Network.Remotes.FirstOrDefault();
                        }
                    }

                    if (remote != null)
                    {
                        var options = new FetchOptions();

                        // Set up credential provider
                        options.CredentialsProvider = (_url, _user, _cred) =>
                        {
                            var token = _tokenManager.GetToken();
                            if (!string.IsNullOrEmpty(token))
                            {
                                return new UsernamePasswordCredentials
                                {
                                    Username = "x-access-token", // GitHub convention for token as username
                                    Password = token
                                };
                            }
                            // Return default credentials if no token found (e.g. for public repos)
                            return new DefaultCredentials();
                        };

                        var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                        Commands.Fetch(repo, remote.Name, refSpecs, options, null);
                    }
                }
            });
        }

        public Task<DiffModel> GetFileDiffModelAsync(string repoPath, string filePath, bool staged)
        {
            return Task.Run(async () =>
            {
                var rawDiff = await GetFileDiffAsync(repoPath, filePath, staged);
                return ParseDiff(filePath, rawDiff);
            });
        }

        public Task StageLinesAsync(string repoPath, string filePath, IEnumerable<DiffLine> selectedLines, IEnumerable<DiffHunk> allHunks)
        {
            return Task.Run(() =>
            {
                // Check if file is untracked (NewInWorkdir). If so, we need to 'git add -N' (intent-to-add)
                // it first, otherwise 'git apply --cached' will complain that the file is not in the index.
                using (var repo = new Repository(repoPath))
                {
                    var status = repo.RetrieveStatus(filePath);
                    if (status == FileStatus.NewInWorkdir)
                    {
                        RunGitCommand(repoPath, "add -N", filePath);
                    }
                }

                var patch = GeneratePatch(filePath, selectedLines.ToList(), allHunks.ToList(), isUnstaging: false);
                ApplyPatch(repoPath, patch, cached: true, reverse: false);
            });
        }

        public Task UnstageLinesAsync(string repoPath, string filePath, IEnumerable<DiffLine> selectedLines, IEnumerable<DiffHunk> allHunks)
        {
            return Task.Run(() =>
            {
                var patch = GeneratePatch(filePath, selectedLines.ToList(), allHunks.ToList(), isUnstaging: true);
                ApplyPatch(repoPath, patch, cached: true, reverse: true);
            });
        }

        private static void RunGitCommand(string repoPath, string command, string filePath)
        {
             var args = $"{command} \"{filePath}\"";
             var psi = new ProcessStartInfo("git", args)
             {
                 WorkingDirectory = repoPath,
                 RedirectStandardOutput = true,
                 RedirectStandardError = true,
                 UseShellExecute = false,
                 CreateNoWindow = true
             };
             
             using var process = Process.Start(psi);
             if (process != null)
             {
                 process.WaitForExit();
                 if (process.ExitCode != 0)
                 {
                     var error = process.StandardError.ReadToEnd();
                     throw new InvalidOperationException($"git {command} failed: {error}");
                 }
             }
        }

        // --- Private helpers ---

        private static DiffModel ParseDiff(string filePath, string rawDiff)
        {
            var model = new DiffModel { FilePath = filePath };
            if (string.IsNullOrWhiteSpace(rawDiff) || rawDiff == "No changes.")
                return model;

            var hunkHeaderRegex = new Regex(@"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@", RegexOptions.Compiled);
            DiffHunk? currentHunk = null;
            int oldLine = 0, newLine = 0;

            foreach (var rawLine in rawDiff.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                var match = hunkHeaderRegex.Match(line);
                if (match.Success)
                {
                    currentHunk = new DiffHunk
                    {
                        Header = line,
                        OldStart = int.Parse(match.Groups[1].Value),
                        OldCount = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1,
                        NewStart = int.Parse(match.Groups[3].Value),
                        NewCount = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 1,
                    };
                    model.Hunks.Add(currentHunk);
                    oldLine = currentHunk.OldStart;
                    newLine = currentHunk.NewStart;
                    continue;
                }

                if (currentHunk == null) continue;

                if (line.StartsWith("+") && !line.StartsWith("+++"))
                {
                    currentHunk.Lines.Add(new DiffLine { Content = line[1..], Type = DiffLineType.Addition, NewLineNumber = newLine++ });
                }
                else if (line.StartsWith("-") && !line.StartsWith("---"))
                {
                    currentHunk.Lines.Add(new DiffLine { Content = line[1..], Type = DiffLineType.Deletion, OldLineNumber = oldLine++ });
                }
                else if (line.StartsWith(" "))
                {
                    currentHunk.Lines.Add(new DiffLine { Content = line[1..], Type = DiffLineType.Context, OldLineNumber = oldLine++, NewLineNumber = newLine++ });
                }
            }

            return model;
        }

        /// <summary>
        /// Generates a minimal unified diff patch for the selected lines.
        /// </summary>
        private static string GeneratePatch(string filePath, List<DiffLine> selectedLines, List<DiffHunk> allHunks, bool isUnstaging)
        {
            var selectedSet = new HashSet<DiffLine>(selectedLines);
            var sb = new StringBuilder();
            sb.Append($"diff --git a/{filePath} b/{filePath}\n");
            sb.Append($"--- a/{filePath}\n");
            sb.Append($"+++ b/{filePath}\n");

            foreach (var hunk in allHunks)
            {
                // Only include hunks that have at least one selected line
                var hunkSelected = hunk.Lines.Where(l => selectedSet.Contains(l) && l.Type != DiffLineType.Context).ToList();
                if (hunkSelected.Count == 0) continue;

                var outputLines = new List<(string prefix, string content)>();
                int oldCount = 0, newCount = 0;

                foreach (var dl in hunk.Lines)
                {
                    if (dl.Type == DiffLineType.Context)
                    {
                        outputLines.Add((" ", dl.Content));
                        oldCount++;
                        newCount++;
                    }
                    else if (dl.Type == DiffLineType.Addition)
                    {
                        if (selectedSet.Contains(dl))
                        {
                            // Selected: Include the change
                            outputLines.Add(("+", dl.Content));
                            newCount++;
                        }
                        else
                        {
                            // Unselected Addition
                            if (isUnstaging)
                            {
                                // Unstaging: The line exists in the Index. Treat as context.
                                outputLines.Add((" ", dl.Content));
                                oldCount++;
                                newCount++;
                            }
                            else
                            {
                                // Staging: The line is NOT in the Index. Skip it.
                            }
                        }
                    }
                    else if (dl.Type == DiffLineType.Deletion)
                    {
                        if (selectedSet.Contains(dl))
                        {
                            // Selected: Include the change (deletion)
                            outputLines.Add(("-", dl.Content));
                            oldCount++;
                        }
                        else
                        {
                            // Unselected Deletion
                            if (isUnstaging)
                            {
                                // Unstaging: The line is DELETED in the Index. It does not exist there. Skip.
                            }
                            else
                            {
                                // Staging: The line exists in the Index (not deleted yet). Treat as context.
                                outputLines.Add((" ", dl.Content));
                                oldCount++;
                                newCount++;
                            }
                        }
                    }
                }

                if (outputLines.Count > 0)
                {
                    sb.Append($"@@ -{hunk.OldStart},{oldCount} +{hunk.NewStart},{newCount} @@\n");
                    foreach (var (prefix, content) in outputLines)
                        sb.Append($"{prefix}{content}\n");
                }
            }

            return sb.ToString();
        }

        private static void ApplyPatch(string repoPath, string patchContent, bool cached, bool reverse = false)
        {
            var args = "apply --whitespace=nowarn --ignore-space-change --ignore-whitespace --recount";
            if (cached) args += " --cached";
            if (reverse) args += " --reverse";

            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = repoPath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start git process.");
            process.StandardInput.Write(patchContent);
            process.StandardInput.Close();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"git apply failed: {error}\nPatch Content:\n{patchContent}");
            }
        }
    }
}
