using System;
using System.Runtime.InteropServices;
using System.Text;
using FlaUI.Core.AutomationElements;
using YasGMP.Wpf.Automation;

namespace YasGMP.Wpf.Smoke.Helpers;

internal static class SmokeAutomationClient
{
    private const int WmCopyData = 0x004A;

    public static void SetLanguage(Window window, string language)
        => Send(window, SmokeAutomationCommand.SetLanguage, language);

    public static void ResetInspector(Window window)
        => Send(window, SmokeAutomationCommand.ResetInspector, string.Empty);

    private static void Send(Window window, SmokeAutomationCommand command, string payload)
    {
        var handle = window.FrameworkAutomationElement.NativeWindowHandle;
        if (handle is null)
        {
            throw new InvalidOperationException("Window handle is not available.");
        }

        var message = string.IsNullOrEmpty(payload) ? "\0" : payload + "\0";
        var bytes = Encoding.Unicode.GetBytes(message);
        var data = new CopyDataStruct
        {
            dwData = (IntPtr)command,
            cbData = bytes.Length,
            lpData = Marshal.AllocHGlobal(bytes.Length),
        };

        try
        {
            Marshal.Copy(bytes, 0, data.lpData, bytes.Length);
            SendMessage(new IntPtr(handle.Value), WmCopyData, IntPtr.Zero, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(data.lpData);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref CopyDataStruct lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct CopyDataStruct
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }
}
