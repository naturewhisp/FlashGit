using System.Collections.Generic;

namespace TurboGit.Core.Models
{
    public enum DiffLineType
    {
        Context,
        Addition,
        Deletion,
        HunkHeader
    }

    public class DiffLine
    {
        public string Content { get; set; } = string.Empty;
        public DiffLineType Type { get; set; }
        public int? OldLineNumber { get; set; }
        public int? NewLineNumber { get; set; }
        /// <summary>Index within the hunk, used for patch generation.</summary>
        public int HunkIndex { get; set; }
    }

    public class DiffHunk
    {
        public int Index { get; set; }
        public string Header { get; set; } = string.Empty;
        public int OldStart { get; set; }
        public int OldCount { get; set; }
        public int NewStart { get; set; }
        public int NewCount { get; set; }
        public List<DiffLine> Lines { get; set; } = new();
    }

    public class DiffModel
    {
        public string FilePath { get; set; } = string.Empty;
        public List<DiffHunk> Hunks { get; set; } = new();

        public IEnumerable<DiffLine> AllLines()
        {
            foreach (var hunk in Hunks)
                foreach (var line in hunk.Lines)
                    yield return line;
        }
    }
}
