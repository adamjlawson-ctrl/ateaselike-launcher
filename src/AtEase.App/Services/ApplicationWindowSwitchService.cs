using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AtEase.App.Services;

public class ApplicationWindowSwitchService
{
    private nint _launcherWindowHandle;

    public void RegisterLauncherWindow(nint hwnd)
    {
        _launcherWindowHandle = hwnd;
    }

    public bool BringLauncherToFront()
    {
        return BringWindowToFront(_launcherWindowHandle);
    }

    public bool TryBringProcessToFront(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                return false;
            }

            for (var i = 0; i < 5; i++)
            {
                process.Refresh();
                var handle = process.MainWindowHandle;
                if (BringWindowToFront(handle))
                {
                    return true;
                }

                Thread.Sleep(75);
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public int? GetForegroundProcessId()
    {
        var foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == 0)
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(foregroundWindow, out var processId);
        if (processId == 0)
        {
            return null;
        }

        return unchecked((int)processId);
    }

    private static bool BringWindowToFront(nint hwnd)
    {
        if (hwnd == 0)
        {
            return false;
        }

        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
        return NativeMethods.SetForegroundWindow(hwnd);
    }

    private static class NativeMethods
    {
        public const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        public static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(nint hWnd, int nCmdShow);
    }
}
