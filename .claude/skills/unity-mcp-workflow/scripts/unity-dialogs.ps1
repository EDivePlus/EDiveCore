# Windows only. Reports whether a Unity editor is blocked by a modal window (which also blocks every MCP call),
# lists the blocking windows with their buttons, and can dismiss them.
# A modal disables the editor's main window, so "blocked" works for native dialogs and Unity's own modal windows alike.
#   unity-dialogs.ps1                      list Unity editors and their blocking windows
#   unity-dialogs.ps1 -Title "Missing Signature" -Close
#   unity-dialogs.ps1 -Title "Scene" -Button "Don't Save"
#   unity-dialogs.ps1 -Title "Missing Signature" -Capture          save a PNG of the window (also Unity's own windows) to read it
#   unity-dialogs.ps1 -Title "Missing Signature" -ClickAt 420,310  click a point in window pixels, as seen in the capture
param(
    [string]$Title,
    [string]$Button,
    [switch]$Close,
    [switch]$Capture,
    [int[]]$ClickAt,
    [string]$CaptureDir = (Join-Path $env:TEMP 'unity-dialogs')
)
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public static class UnityDialogs {
    public delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc p, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, EnumProc p, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern void keybd_event(byte k, byte s, uint f, UIntPtr e);
    // Unity paints a modal window only while it is in the foreground; the Alt tap lets a background process take focus
    public static void Focus(IntPtr h) { keybd_event(0x12, 0, 0, UIntPtr.Zero); keybd_event(0x12, 0, 2, UIntPtr.Zero); SetForegroundWindow(h); }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] public static extern IntPtr ChildWindowFromPointEx(IntPtr parent, POINT p, uint flags);
    // Posts a left click to the deepest child window at a client point of the given window
    public static IntPtr Click(IntPtr window, int x, int y) {
        var target = window; var pt = new POINT { X = x, Y = y };
        while (true) {
            var child = ChildWindowFromPointEx(target, pt, 1 | 4);
            if (child == IntPtr.Zero || child == target) break;
            var screen = pt; ClientToScreen(target, ref screen);
            ScreenToClient(child, ref screen);
            target = child; pt = screen;
        }
        var lp = (IntPtr)((pt.Y << 16) | (pt.X & 0xFFFF));
        PostMessage(target, 0x0201, (IntPtr)1, lp);
        PostMessage(target, 0x0202, IntPtr.Zero, lp);
        return target;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")] static extern IntPtr SendText(IntPtr h, uint msg, IntPtr w, StringBuilder l);
    // GetWindowText can't read controls of another process (e.g. the dialog's Edit box), WM_GETTEXT can
    public static string ControlText(IntPtr h) { var s = new StringBuilder(2048); SendText(h, 0x000D, (IntPtr)2048, s); return s.ToString(); }
    public static string Text(IntPtr h) { var s = new StringBuilder(512); GetWindowText(h, s, 512); return s.ToString(); }
    public static string Class(IntPtr h) { var s = new StringBuilder(256); GetClassName(h, s, 256); return s.ToString(); }
    public static List<IntPtr> TopWindows(uint pid) {
        var r = new List<IntPtr>();
        EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) r.Add(h); return true; }, IntPtr.Zero);
        return r;
    }
    public static List<IntPtr> Children(IntPtr parent) {
        var r = new List<IntPtr>();
        EnumChildWindows(parent, (h, l) => { r.Add(h); return true; }, IntPtr.Zero);
        return r;
    }
}
"@

$WM_CLOSE = 0x0010
$BM_CLICK = 0x00F5
$found = $false

foreach ($proc in Get-Process Unity -ErrorAction SilentlyContinue) {
    $main = $proc.MainWindowHandle
    # Asset import workers and similar helpers have no windows at all
    if ($main -eq [IntPtr]::Zero -and [UnityDialogs]::TopWindows([uint32]$proc.Id).Count -eq 0) { continue }
    if ($main -eq [IntPtr]::Zero) { "PID $($proc.Id): no main window yet (starting up), showing all its windows" }
    # At startup a native dialog (e.g. scene backup recovery) can be the process's main window itself
    $mainIsDialog = $main -ne [IntPtr]::Zero -and [UnityDialogs]::Class($main) -eq '#32770'
    $blocked = $main -ne [IntPtr]::Zero -and ($mainIsDialog -or -not [UnityDialogs]::IsWindowEnabled($main))
    if ($main -ne [IntPtr]::Zero) { "PID $($proc.Id) blocked=$blocked editor='$($proc.MainWindowTitle)'" }

    foreach ($h in [UnityDialogs]::TopWindows([uint32]$proc.Id)) {
        if ($h -eq $main -and -not $mainIsDialog) { continue }
        $text = [UnityDialogs]::Text($h)
        $class = [UnityDialogs]::Class($h)
        # Native dialogs always count; other top-level windows only while the editor is blocked, and floating editor windows are disabled then too
        if ($class -ne '#32770' -and -not ($blocked -or $main -eq [IntPtr]::Zero)) { continue }
        if ($class -ne '#32770' -and $blocked -and -not [UnityDialogs]::IsWindowEnabled($h)) { continue }
        if ([string]::IsNullOrWhiteSpace($text) -and $class -ne '#32770') { continue }
        $found = $true

        $children = [UnityDialogs]::Children($h)
        $buttons = @($children | Where-Object { [UnityDialogs]::Class($_) -eq 'Button' })
        $labels = @($children | Where-Object { [UnityDialogs]::Class($_) -in 'Static', 'Edit' } | ForEach-Object { [UnityDialogs]::ControlText($_) } | Where-Object { $_ })
        "  window='$text' class=$class"
        if ($labels) { "    text: " + ($labels -join ' | ') }
        if ($buttons) { "    buttons: " + (($buttons | ForEach-Object { [UnityDialogs]::Text($_) }) -join ' | ') }
        elseif ($class -ne '#32770') { "    (Unity window: read it with -Capture, answer with -ClickAt or -Close)" }

        if ($Title -and $text -like "*$Title*") {
            if ($Capture) {
                [UnityDialogs]::Focus($h); Start-Sleep -Milliseconds 700
                $r = New-Object UnityDialogs+RECT
                [void][UnityDialogs]::GetWindowRect($h, [ref]$r)
                $bmp = New-Object System.Drawing.Bitmap ([Math]::Max(1, $r.Right - $r.Left)), ([Math]::Max(1, $r.Bottom - $r.Top))
                $g = [System.Drawing.Graphics]::FromImage($bmp)
                $hdc = $g.GetHdc()
                # Flag 2 (full content) is needed for Unity windows, a plain capture comes out blank
                [void][UnityDialogs]::PrintWindow($h, $hdc, 2)
                $g.ReleaseHdc($hdc); $g.Dispose()
                New-Item -ItemType Directory -Force $CaptureDir | Out-Null
                $file = Join-Path $CaptureDir (($text -replace '[^A-Za-z0-9]+', '_') + '.png')
                $bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
                "    -> captured $file"
            }
            if ($ClickAt -and $ClickAt.Count -eq 2) {
                # Capture pixels include the frame: convert to client coordinates
                $wr = New-Object UnityDialogs+RECT; $cr = New-Object UnityDialogs+RECT
                [void][UnityDialogs]::GetWindowRect($h, [ref]$wr); [void][UnityDialogs]::GetClientRect($h, [ref]$cr)
                $border = [int](($wr.Right - $wr.Left - $cr.Right) / 2)
                $titleBar = ($wr.Bottom - $wr.Top - $cr.Bottom) - $border
                $target = [UnityDialogs]::Click($h, $ClickAt[0] - $border, $ClickAt[1] - $titleBar)
                "    -> clicked at $($ClickAt -join ',') on '$([UnityDialogs]::Text($target))'"
            }
            if ($Button) {
                $target = $buttons | Where-Object { ([UnityDialogs]::Text($_) -replace '&', '') -like "*$Button*" } | Select-Object -First 1
                if ($target) { [void][UnityDialogs]::SendMessage($target, $BM_CLICK, [IntPtr]::Zero, [IntPtr]::Zero); "    -> clicked '$Button'" }
                else { "    -> no button '$Button'" }
            }
            elseif ($Close) {
                [void][UnityDialogs]::PostMessage($h, $WM_CLOSE, [IntPtr]::Zero, [IntPtr]::Zero); "    -> closed"
            }
        }
    }
}

if (-not $found) { "No blocking windows." }
