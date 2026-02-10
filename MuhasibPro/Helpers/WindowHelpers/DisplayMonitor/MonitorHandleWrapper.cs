using Windows.Win32.Graphics.Gdi;

namespace MuhasibPro.Helpers.WindowHelpers.DisplayMonitor;

internal sealed class MonitorHandleWrapper
{
    public HMONITOR Handle;
    public MonitorInfo? Info;
}
