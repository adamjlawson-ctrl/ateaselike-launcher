using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using AtEase.App.Models;
using System.Diagnostics;
using System.Reflection;

namespace AtEase.App.Services;

public sealed class CuratedAppDiscoveryService
{
    // Temporary debug mode: prove baseline discovery before exclusion/dedupe filtering.
    private const bool DisableExclusionAndDedupeTemporarily = true;

    private static readonly string[] LaunchableTargetExtensions = [".exe", ".bat", ".cmd", ".com"];

    private static readonly string[] ShortcutExtensions = [".lnk"];

    public IReadOnlyList<AppPickerEntry> DiscoverCandidates(IReadOnlyCollection<string> excludedLaunchPaths, Action<string>? diagnosticsCallback = null)
    {
        var shortcutFilesFound = 0;
        var resolvedEntries = new List<AppPickerEntry>();
        var folderReports = new List<FolderScanReport>();

        var resolveAttempts = 0;
        var resolveSucceeded = 0;
        var resolveFailed = 0;
        var rejectedMissingTarget = 0;
        var rejectedNonLaunchableTarget = 0;
        var rejectedMissingFile = 0;

        var resolvedSamples = new List<string>();
        var failedSamples = new List<string>();

        // Stage 1 + 2: scan Start Menu shortcuts and resolve plausible targets.
        foreach (var source in EnumerateCuratedSources())
        {
            if (!Directory.Exists(source.Path))
            {
                folderReports.Add(new FolderScanReport(source.Path, source.Label, Exists: false, TotalFiles: 0, ShortcutFiles: 0, SubfoldersScanned: 0));
                continue;
            }

            var (shortcutFilesInFolder, report) = ScanShortcutFiles(source.Path);
            foreach (var file in shortcutFilesInFolder)
            {
                shortcutFilesFound++;
                resolveAttempts++;

                if (!TryBuildEntry(file, source.Label, out var entry, out var rejectionReason))
                {
                    if (rejectionReason == RejectionReason.ResolveFailed)
                    {
                        resolveFailed++;
                    }
                    else if (rejectionReason == RejectionReason.MissingTarget)
                    {
                        rejectedMissingTarget++;
                    }
                    else if (rejectionReason == RejectionReason.NonLaunchableTarget)
                    {
                        rejectedNonLaunchableTarget++;
                    }
                    else if (rejectionReason == RejectionReason.MissingShortcutFile)
                    {
                        rejectedMissingFile++;
                    }

                    if (failedSamples.Count < 5)
                    {
                        failedSamples.Add(file);
                    }

                    continue;
                }

                resolveSucceeded++;
                if (resolvedSamples.Count < 5)
                {
                    resolvedSamples.Add($"{entry.DisplayName} -> {entry.LaunchPath}");
                }

                resolvedEntries.Add(entry);
            }

            folderReports.Add(report with { Label = source.Label });
        }

        var rejectedEntries = Math.Max(0, shortcutFilesFound - resolvedEntries.Count);

        // Stage 3: diagnostics before filtering.
        var summary = $"Discovery pre-filter: scanned={shortcutFilesFound}, resolved={resolvedEntries.Count}, rejected={rejectedEntries}.";
        Debug.WriteLine($"[AtEase][AppPicker] {summary}");

        foreach (var report in folderReports)
        {
            Debug.WriteLine($"[AtEase][AppPicker] Scan folder: label={report.Label}, path={report.Path}, exists={report.Exists}, totalFiles={report.TotalFiles}, lnkFiles={report.ShortcutFiles}, subfolders={report.SubfoldersScanned}");
        }

        Debug.WriteLine($"[AtEase][AppPicker] Resolve: attempted={resolveAttempts}, succeeded={resolveSucceeded}, failed={resolveFailed}");
        Debug.WriteLine($"[AtEase][AppPicker] Reject reasons: missingTarget={rejectedMissingTarget}, nonLaunchableTarget={rejectedNonLaunchableTarget}, missingShortcutFile={rejectedMissingFile}");

        if (resolvedSamples.Count == 0)
        {
            Debug.WriteLine("[AtEase][AppPicker] Resolved samples: (none)");
        }
        else
        {
            foreach (var item in resolvedSamples)
            {
                Debug.WriteLine($"[AtEase][AppPicker] Resolved sample: {item}");
            }
        }

        if (failedSamples.Count == 0)
        {
            Debug.WriteLine("[AtEase][AppPicker] Failed samples: (none)");
        }
        else
        {
            foreach (var item in failedSamples)
            {
                Debug.WriteLine($"[AtEase][AppPicker] Failed sample: {item}");
            }
        }

        diagnosticsCallback?.Invoke(summary);

        // Stage 5 + 6: exclude already selected apps, then dedupe by resolved path.
        var excluded = excludedLaunchPaths
            .Select(NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<AppPickerEntry>();
        var excludedCount = 0;
        var duplicateCount = 0;

        foreach (var entry in resolvedEntries)
        {
            var launchPath = NormalizePath(entry.LaunchPath);
            var resolvedPath = NormalizePath(entry.ResolvedTargetPath);

            if (!DisableExclusionAndDedupeTemporarily &&
                (excluded.Contains(launchPath) || (!string.IsNullOrWhiteSpace(resolvedPath) && excluded.Contains(resolvedPath))))
            {
                excludedCount++;
                continue;
            }

            var dedupeKey = !string.IsNullOrWhiteSpace(resolvedPath)
                ? $"target::{resolvedPath}"
                : $"launch::{launchPath}";

            if (!DisableExclusionAndDedupeTemporarily && !dedupe.Add(dedupeKey))
            {
                duplicateCount++;
                continue;
            }

            _ = dedupe.Add(dedupeKey);
            entries.Add(entry);
        }

        var filterSummary = $"Discovery post-filter: final={entries.Count}, excludedAsSelected={excludedCount}, removedAsDuplicate={duplicateCount}, tempFilterBypass={DisableExclusionAndDedupeTemporarily}.";
        Debug.WriteLine($"[AtEase][AppPicker] {filterSummary}");
        diagnosticsCallback?.Invoke($"scanned {shortcutFilesFound} shortcuts, resolved {resolveSucceeded}, final usable apps {entries.Count}");

        return entries
            .OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.LaunchPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<(string Path, string Label)> EnumerateCuratedSources()
    {
        var userPrograms = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs");
        if (!string.IsNullOrWhiteSpace(userPrograms))
        {
            yield return (userPrograms, "User Start Menu");
        }

        var commonPrograms = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            "Programs");
        if (!string.IsNullOrWhiteSpace(commonPrograms))
        {
            yield return (commonPrograms, "All Users Start Menu");
        }
    }

    private static (List<string> ShortcutFiles, FolderScanReport Report) ScanShortcutFiles(string root)
    {
        var queue = new Queue<string>();
        queue.Enqueue(root);
        var subfoldersScanned = 0;
        var totalFiles = 0;
        var shortcutFiles = 0;
        var shortcuts = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
            {
                subfoldersScanned++;
            }

            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(current);
            }
            catch
            {
                directories = [];
            }

            foreach (var directory in directories)
            {
                queue.Enqueue(directory);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(current);
            }
            catch
            {
                files = [];
            }

            foreach (var file in files)
            {
                totalFiles++;
                var extension = Path.GetExtension(file);
                if (ShortcutExtensions.Any(x => extension.Equals(x, StringComparison.OrdinalIgnoreCase)))
                {
                    shortcutFiles++;
                    shortcuts.Add(file);
                }
            }
        }

        return (
            shortcuts,
            new FolderScanReport(root, Label: string.Empty, Exists: true, TotalFiles: totalFiles, ShortcutFiles: shortcutFiles, SubfoldersScanned: subfoldersScanned));
    }

    private static bool TryBuildEntry(string shortcutPath, string source, out AppPickerEntry entry, out RejectionReason rejectionReason)
    {
        entry = null!;
        rejectionReason = RejectionReason.Unknown;

        if (!File.Exists(shortcutPath))
        {
            rejectionReason = RejectionReason.MissingShortcutFile;
            return false;
        }

        var extension = Path.GetExtension(shortcutPath);
        if (!ShortcutExtensions.Any(x => extension.Equals(x, StringComparison.OrdinalIgnoreCase)))
        {
            rejectionReason = RejectionReason.NonShortcutType;
            return false;
        }

        var displayName = Path.GetFileNameWithoutExtension(shortcutPath);
        if (!TryResolveShortcut(shortcutPath, out var resolvedTargetPath))
        {
            rejectionReason = RejectionReason.ResolveFailed;
            return false;
        }

        if (string.IsNullOrWhiteSpace(resolvedTargetPath) || !File.Exists(resolvedTargetPath))
        {
            rejectionReason = RejectionReason.MissingTarget;
            return false;
        }

        var targetExtension = Path.GetExtension(resolvedTargetPath);
        if (!LaunchableTargetExtensions.Any(x => targetExtension.Equals(x, StringComparison.OrdinalIgnoreCase)))
        {
            rejectionReason = RejectionReason.NonLaunchableTarget;
            return false;
        }

        entry = new AppPickerEntry
        {
            DisplayName = displayName,
            LaunchPath = resolvedTargetPath,
            ResolvedTargetPath = resolvedTargetPath,
            IconHint = resolvedTargetPath,
            Source = source
        };

        rejectionReason = RejectionReason.None;
        return true;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.Trim();
    }

    private static bool TryResolveShortcut(string shortcutPath, out string targetPath)
    {
        if (TryResolveShortcutViaShellLink(shortcutPath, out targetPath))
        {
            return true;
        }

        return TryResolveShortcutViaWshShell(shortcutPath, out targetPath);
    }

    private static bool TryResolveShortcutViaShellLink(string shortcutPath, out string targetPath)
    {
        targetPath = string.Empty;

        IShellLinkW? shellLink = null;
        IPersistFile? persistFile = null;

        try
        {
            shellLink = (IShellLinkW)new ShellLink();
            persistFile = (IPersistFile)shellLink;
            persistFile.Load(shortcutPath, 0);
            shellLink.Resolve(0, 0x1);

            var buffer = new char[1024];
            shellLink.GetPath(buffer, buffer.Length, out _, 0x4);
            var resolved = new string(buffer).TrimEnd('\0').Trim();

            if (string.IsNullOrWhiteSpace(resolved))
            {
                return false;
            }

            targetPath = Environment.ExpandEnvironmentVariables(resolved);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (persistFile is not null)
            {
                Marshal.ReleaseComObject(persistFile);
            }

            if (shellLink is not null)
            {
                Marshal.ReleaseComObject(shellLink);
            }
        }
    }

    private static bool TryResolveShortcutViaWshShell(string shortcutPath, out string targetPath)
    {
        targetPath = string.Empty;
        object? wshShell = null;
        object? shortcut = null;

        try
        {
            var wshType = Type.GetTypeFromProgID("WScript.Shell");
            if (wshType is null)
            {
                return false;
            }

            wshShell = Activator.CreateInstance(wshType);
            if (wshShell is null)
            {
                return false;
            }

            shortcut = wshType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                wshShell,
                [shortcutPath]);

            if (shortcut is null)
            {
                return false;
            }

            var shortcutType = shortcut.GetType();
            var rawTarget = shortcutType.InvokeMember(
                "TargetPath",
                BindingFlags.GetProperty,
                null,
                shortcut,
                null) as string;

            if (string.IsNullOrWhiteSpace(rawTarget))
            {
                return false;
            }

            targetPath = Environment.ExpandEnvironmentVariables(rawTarget.Trim());
            return !string.IsNullOrWhiteSpace(targetPath);
        }
        catch
        {
            return false;
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut))
            {
                Marshal.ReleaseComObject(shortcut);
            }

            if (wshShell is not null && Marshal.IsComObject(wshShell))
            {
                Marshal.ReleaseComObject(wshShell);
            }
        }
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath(
            [Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszFile,
            int cchMaxPath,
            out WIN32_FIND_DATAW pfd,
            int fFlags);

        void GetIDList(out nint ppidl);

        void SetIDList(nint pidl);

        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszName, int cchMaxName);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszDir, int cchMaxPath);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszArgs, int cchMaxPath);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        void GetHotkey(out short pwHotkey);

        void SetHotkey(short wHotkey);

        void GetShowCmd(out int piShowCmd);

        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszIconPath, int cchIconPath, out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

        void Resolve(nint hwnd, int fFlags);

        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);

        [PreserveSig]
        int IsDirty();

        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);

        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);

        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    private enum RejectionReason
    {
        None,
        Unknown,
        NonShortcutType,
        MissingShortcutFile,
        ResolveFailed,
        MissingTarget,
        NonLaunchableTarget
    }

    private readonly record struct FolderScanReport(
        string Path,
        string Label,
        bool Exists,
        int TotalFiles,
        int ShortcutFiles,
        int SubfoldersScanned);
}
