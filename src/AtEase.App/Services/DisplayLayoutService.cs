using System.Diagnostics;
using System.Runtime.InteropServices;
using AtEase.App.Models;

namespace AtEase.App.Services;

public class DisplayLayoutService
{
    public HashSet<nint> SnapshotTopLevelWindowHandles()
    {
        var handles = new HashSet<nint>();

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (hwnd == 0)
            {
                return true;
            }

            handles.Add(hwnd);
            return true;
        }, 0);

        return handles;
    }

    public HashSet<int> SnapshotProcessIdsForExecutable(string executablePath)
    {
        var exeName = Path.GetFileNameWithoutExtension(executablePath);
        if (string.IsNullOrWhiteSpace(exeName))
        {
            return [];
        }

        return Process.GetProcessesByName(exeName)
            .Select(p =>
            {
                try
                {
                    return p.Id;
                }
                catch
                {
                    return -1;
                }
            })
            .Where(id => id > 0)
            .ToHashSet();
    }

    public IReadOnlyList<DisplayTarget> GetDisplays()
    {
        var entries = new List<DisplayTarget>();

        NativeMethods.EnumDisplayMonitors(0, 0, (nint monitorHandle, nint _, ref NativeMethods.RECT rect, nint __) =>
        {
            var info = new NativeMethods.MONITORINFOEX();
            info.cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>();

            if (!NativeMethods.GetMonitorInfo(monitorHandle, ref info))
            {
                return true;
            }

            entries.Add(new DisplayTarget
            {
                Id = info.szDevice,
                IsPrimary = (info.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0,
                Left = info.rcMonitor.Left,
                Top = info.rcMonitor.Top,
                Width = info.rcMonitor.Right - info.rcMonitor.Left,
                Height = info.rcMonitor.Bottom - info.rcMonitor.Top
            });

            return true;
        }, 0);

        var ordered = entries
            .OrderByDescending(d => d.IsPrimary)
            .ThenBy(d => d.Left)
            .ThenBy(d => d.Top)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Label = $"Display {i + 1}";
        }

        return ordered;
    }

    public bool TryMoveAndMaximizeProcessWindow(
        int processId,
        string executablePath,
        HashSet<int> preLaunchProcessIds,
        HashSet<nint> preLaunchWindowHandles,
        DateTime launchUtc,
        DisplayTarget target)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                return false;
            }

            if (!TryGetMonitorInfoByDeviceId(target.Id, out var targetMonitorInfo))
            {
                Debug.WriteLine($"[AtEase] Display target missing: id={target.Id}, label={target.Label}");
                return false;
            }

            LogSelectedMonitor(target, targetMonitorInfo);

            WaitForInputIdleSafe(process, 1500);

            var executableName = Path.GetFileNameWithoutExtension(executablePath);
            for (var attempt = 0; attempt < 60; attempt++)
            {
                process.Refresh();
                var hwnd = FindBestLaunchedWindowHandle(
                    process,
                    executableName,
                    preLaunchProcessIds,
                    preLaunchWindowHandles,
                    launchUtc);
                if (hwnd != 0)
                {
                    Debug.WriteLine($"[AtEase] Window handle found: attempt={attempt + 1}, hwnd=0x{hwnd.ToInt64():X}");

                    var movedToTarget = PlaceWindowOnDisplay(hwnd, targetMonitorInfo);
                    var onTargetAfterMove = IsWindowOnTargetDisplay(hwnd, target.Id);
                    Debug.WriteLine($"[AtEase] Placement result: moved={movedToTarget}, onTargetAfterMove={onTargetAfterMove}");

                    if (onTargetAfterMove)
                    {
                        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_MAXIMIZE);
                        var onTargetAfterMaximize = IsWindowOnTargetDisplay(hwnd, target.Id);
                        Debug.WriteLine($"[AtEase] Maximize after placement: hwnd=0x{hwnd.ToInt64():X}, onTargetAfterMaximize={onTargetAfterMaximize}");

                        if (!onTargetAfterMaximize)
                        {
                            Thread.Sleep(350);

                            var secondPlacement = PlaceWindowOnDisplay(hwnd, targetMonitorInfo);
                            var onTargetAfterRetryMove = IsWindowOnTargetDisplay(hwnd, target.Id);
                            Debug.WriteLine($"[AtEase] Delayed reapply: moved={secondPlacement}, onTargetAfterRetryMove={onTargetAfterRetryMove}");

                            if (onTargetAfterRetryMove)
                            {
                                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_MAXIMIZE);
                                onTargetAfterMaximize = IsWindowOnTargetDisplay(hwnd, target.Id);
                                Debug.WriteLine($"[AtEase] Maximize after delayed reapply: onTargetAfterMaximize={onTargetAfterMaximize}");
                            }
                        }

                        var finalMonitorId = GetWindowMonitorDeviceId(hwnd) ?? "unknown";
                        Debug.WriteLine($"[AtEase] Final monitor: {finalMonitorId}");

                        if (onTargetAfterMaximize)
                        {
                            return true;
                        }
                    }
                }

                Thread.Sleep(150);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void WaitForInputIdleSafe(Process process, int timeoutMs)
    {
        try
        {
            _ = process.WaitForInputIdle(timeoutMs);
        }
        catch
        {
            // Some processes do not expose a GUI idle state.
        }
    }

    private static nint FindBestLaunchedWindowHandle(
        Process launchedProcess,
        string executableName,
        HashSet<int> preLaunchProcessIds,
        HashSet<nint> preLaunchWindowHandles,
        DateTime launchUtc)
    {
        if (launchedProcess.MainWindowHandle != 0 && IsUsableTopLevelWindow(launchedProcess.MainWindowHandle, launchedProcess.Id))
        {
            return launchedProcess.MainWindowHandle;
        }

        nint found = 0;
        long foundArea = 0;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out var ownerPid);

            // First choice: window owned by launched process or its newly created sibling process.
            if (!IsCandidateWindowProcess(ownerPid, launchedProcess.Id, executableName, preLaunchProcessIds, launchUtc))
            {
                // Fallback: shell/UWP launches can reuse long-running host processes (e.g. ApplicationFrameHost).
                // In that case target newly created top-level windows after launch.
                if (!IsNewTopLevelWindowCandidate(hwnd, ownerPid, preLaunchWindowHandles, launchedProcess.Id))
                {
                    return true;
                }
            }

            if (!IsUsableTopLevelWindow(hwnd, ownerPid))
            {
                return true;
            }

            var area = GetWindowArea(hwnd);
            if (area > foundArea)
            {
                found = hwnd;
                foundArea = area;
            }

            return true;
        }, 0);

        return found;
    }

    private static bool IsNewTopLevelWindowCandidate(
        nint hwnd,
        int ownerPid,
        HashSet<nint> preLaunchWindowHandles,
        int launchedProcessId)
    {
        if (hwnd == 0)
        {
            return false;
        }

        if (ownerPid <= 0)
        {
            return false;
        }

        if (ownerPid == launchedProcessId)
        {
            return true;
        }

        if (ownerPid == Environment.ProcessId)
        {
            return false;
        }

        return !preLaunchWindowHandles.Contains(hwnd);
    }

    private static bool IsCandidateWindowProcess(
        int pid,
        int launchedProcessId,
        string executableName,
        HashSet<int> preLaunchProcessIds,
        DateTime launchUtc)
    {
        if (pid <= 0)
        {
            return false;
        }

        if (pid == launchedProcessId)
        {
            return true;
        }

        if (preLaunchProcessIds.Contains(pid))
        {
            return false;
        }

        try
        {
            var process = Process.GetProcessById(pid);
            if (process.HasExited)
            {
                return false;
            }

            if (!string.Equals(process.ProcessName, executableName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Accept near-launch child/rehost processes for this executable.
            return process.StartTime.ToUniversalTime() >= launchUtc.AddSeconds(-2);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUsableTopLevelWindow(nint hwnd, int processId)
    {
        if (hwnd == 0)
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var ownerPid);
        if (ownerPid != processId)
        {
            return false;
        }

        if (!NativeMethods.IsWindowVisible(hwnd))
        {
            return false;
        }

        if (NativeMethods.GetWindow(hwnd, NativeMethods.GW_OWNER) != 0)
        {
            return false;
        }

        return true;
    }

    private static long GetWindowArea(nint hwnd)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            return 0;
        }

        var width = Math.Max(0, rect.Right - rect.Left);
        var height = Math.Max(0, rect.Bottom - rect.Top);
        return (long)width * height;
    }

    private static bool PlaceWindowOnDisplay(nint hwnd, NativeMethods.MONITORINFOEX monitorInfo)
    {
        var workRect = monitorInfo.rcWork;
        var normalRect = workRect;

        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);

        var placement = new NativeMethods.WINDOWPLACEMENT
        {
            length = Marshal.SizeOf<NativeMethods.WINDOWPLACEMENT>(),
            showCmd = NativeMethods.SW_SHOWNORMAL,
            ptMinPosition = new NativeMethods.POINT(),
            ptMaxPosition = new NativeMethods.POINT(),
            rcNormalPosition = normalRect
        };

        if (NativeMethods.GetWindowPlacement(hwnd, ref placement))
        {
            placement.showCmd = NativeMethods.SW_SHOWNORMAL;
            placement.rcNormalPosition = normalRect;
        }

        var setPlacement = NativeMethods.SetWindowPlacement(hwnd, ref placement);

        var setPosition = NativeMethods.SetWindowPos(
            hwnd,
            0,
            normalRect.Left,
            normalRect.Top,
            Math.Max(200, normalRect.Right - normalRect.Left),
            Math.Max(200, normalRect.Bottom - normalRect.Top),
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

        return setPlacement || setPosition;
    }

    private static bool IsWindowOnTargetDisplay(nint hwnd, string targetDisplayId)
    {
        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (monitor == 0)
        {
            return false;
        }

        var info = new NativeMethods.MONITORINFOEX
        {
            cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
        };

        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        return string.Equals(info.szDevice, targetDisplayId, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetWindowMonitorDeviceId(nint hwnd)
    {
        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (monitor == 0)
        {
            return null;
        }

        var info = new NativeMethods.MONITORINFOEX
        {
            cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
        };

        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return null;
        }

        return info.szDevice;
    }

    private static bool TryGetMonitorInfoByDeviceId(string deviceId, out NativeMethods.MONITORINFOEX monitorInfo)
    {
        var found = false;
        var resolvedInfo = new NativeMethods.MONITORINFOEX
        {
            cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
        };

        NativeMethods.EnumDisplayMonitors(0, 0, (nint monitorHandle, nint _, ref NativeMethods.RECT __, nint ___) =>
        {
            var info = new NativeMethods.MONITORINFOEX
            {
                cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
            };

            if (!NativeMethods.GetMonitorInfo(monitorHandle, ref info))
            {
                return true;
            }

            if (string.Equals(info.szDevice, deviceId, StringComparison.OrdinalIgnoreCase))
            {
                resolvedInfo = info;
                found = true;
                return false;
            }

            return true;
        }, 0);

        monitorInfo = resolvedInfo;
        return found;
    }

    private static void LogSelectedMonitor(DisplayTarget target, NativeMethods.MONITORINFOEX monitorInfo)
    {
        var bounds = monitorInfo.rcMonitor;
        var work = monitorInfo.rcWork;
        var isPrimary = (monitorInfo.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0;

        Debug.WriteLine(
            $"[AtEase] Selected display: label={target.Label}, id={target.Id}, primary={isPrimary}, " +
            $"bounds=({bounds.Left},{bounds.Top},{bounds.Right - bounds.Left},{bounds.Bottom - bounds.Top}), " +
            $"work=({work.Left},{work.Top},{work.Right - work.Left},{work.Bottom - work.Top})");
    }

    private static class NativeMethods
    {
        public const int MONITORINFOF_PRIMARY = 1;
        public const int MONITOR_DEFAULTTONEAREST = 2;
        public const int GW_OWNER = 4;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_RESTORE = 9;
        public const int SW_MAXIMIZE = 3;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        public delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);
        public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll")]
        public static extern nint MonitorFromWindow(nint hWnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(nint hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(nint hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern nint GetWindow(nint hWnd, uint uCmd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(
            nint hWnd,
            nint hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);
    }
}
