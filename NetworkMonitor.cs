using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

class NetworkMonitor
{
    private long _prevBytesReceived;
    private long _prevBytesSent;
    private readonly Stopwatch _stopwatch;

    public NetworkMonitor()
    {
        _stopwatch = Stopwatch.StartNew();
        GetTotalBytes(out _prevBytesReceived, out _prevBytesSent);
    }

    private static void GetTotalBytes(out long bytesReceived, out long bytesSent)
    {
        bytesReceived = 0;
        bytesSent = 0;
        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                IPv4InterfaceStatistics stats = ni.GetIPv4Statistics();
                bytesReceived += stats.BytesReceived;
                bytesSent += stats.BytesSent;
            }
        }
        catch { }
    }

    public void GetSpeeds(out double downloadBytesPerSec, out double uploadBytesPerSec)
    {
        long currentReceived, currentSent;
        GetTotalBytes(out currentReceived, out currentSent);

        double elapsed = _stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Restart();

        if (elapsed > 0)
        {
            downloadBytesPerSec = (currentReceived - _prevBytesReceived) / elapsed;
            uploadBytesPerSec = (currentSent - _prevBytesSent) / elapsed;
        }
        else
        {
            downloadBytesPerSec = 0;
            uploadBytesPerSec = 0;
        }

        if (downloadBytesPerSec < 0) downloadBytesPerSec = 0;
        if (uploadBytesPerSec < 0) uploadBytesPerSec = 0;

        _prevBytesReceived = currentReceived;
        _prevBytesSent = currentSent;
    }
}
