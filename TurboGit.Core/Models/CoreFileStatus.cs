using System;

namespace TurboGit.Core.Models
{
    [Flags]
    public enum CoreFileStatus
    {
        Unaltered = 0,
        NewInIndex = 1 << 0,
        ModifiedInIndex = 1 << 1,
        DeletedFromIndex = 1 << 2,
        RenamedInIndex = 1 << 3,
        TypeChangeInIndex = 1 << 4,
        NewInWorkdir = 1 << 7,
        ModifiedInWorkdir = 1 << 8,
        DeletedFromWorkdir = 1 << 9,
        TypeChangeInWorkdir = 1 << 10,
        RenamedInWorkdir = 1 << 11,
        Unreadable = 1 << 12,
        Ignored = 1 << 14,
        Conflicted = 1 << 15
    }
}
