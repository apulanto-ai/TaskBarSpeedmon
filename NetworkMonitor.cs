using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;

/// <summary>
/// Measures the combined download/upload rate of all active network adapters.
/// Counters are tracked per adapter, so adapters appearing, disappearing or
/// resetting their counters never produce bogus spikes.
/// </summary>
class NetworkMonitor
{
    public const int HistoryLength = 60;

    private readonly Dictionary<string, long[]> _prev = new Dictionary<string, long[]>();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private readonly double[] _downHistory = new double[HistoryLength];
    private readonly double[] _upHistory   = new double[HistoryLength];
    private int _historyPos;

    public double Download      { get; private set; }
    public double Upload        { get; private set; }
    public long   TotalReceived { get; private set; }
    public long   TotalSent     { get; private set; }
    public double PeakDownload  { get; private set; }
    public double PeakUpload    { get; private set; }

    public NetworkMonitor()
    {
        Sample(false);
    }

    /// <summary>Takes a new measurement. Call roughly once per second.</summary>
    public void Update()
    {
        Sample(true);
    }

    private void Sample(bool computeRates)
    {
        double elapsed = _stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Restart();

        long deltaRx = 0, deltaTx = 0;
        var seen = new HashSet<string>();

        NetworkInterface[] interfaces;
        try { interfaces = NetworkInterface.GetAllNetworkInterfaces(); }
        catch { interfaces = new NetworkInterface[0]; }

        foreach (NetworkInterface ni in interfaces)
        {
            try
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                // GetIPStatistics covers IPv4 + IPv6 (GetIPv4Statistics misses IPv6 traffic).
                IPInterfaceStatistics stats = ni.GetIPStatistics();
                long rx = stats.BytesReceived;
                long tx = stats.BytesSent;

                seen.Add(ni.Id);
                long[] prev;
                if (_prev.TryGetValue(ni.Id, out prev))
                {
                    if (rx >= prev[0]) deltaRx += rx - prev[0];
                    if (tx >= prev[1]) deltaTx += tx - prev[1];
                }
                _prev[ni.Id] = new long[] { rx, tx };
            }
            catch { /* Adapter vanished mid-query */ }
        }

        // Forget adapters that are gone.
        var stale = new List<string>();
        foreach (string id in _prev.Keys)
            if (!seen.Contains(id)) stale.Add(id);
        foreach (string id in stale)
            _prev.Remove(id);

        if (!computeRates) return;

        TotalReceived += deltaRx;
        TotalSent     += deltaTx;

        Download = elapsed > 0.05 ? deltaRx / elapsed : 0;
        Upload   = elapsed > 0.05 ? deltaTx / elapsed : 0;

        if (Download > PeakDownload) PeakDownload = Download;
        if (Upload   > PeakUpload)   PeakUpload   = Upload;

        _downHistory[_historyPos] = Download;
        _upHistory[_historyPos]   = Upload;
        _historyPos = (_historyPos + 1) % HistoryLength;
    }

    /// <summary>Download history, oldest value first.</summary>
    public double[] GetDownloadHistory() { return Ordered(_downHistory); }

    /// <summary>Upload history, oldest value first.</summary>
    public double[] GetUploadHistory()   { return Ordered(_upHistory); }

    private double[] Ordered(double[] ring)
    {
        var result = new double[HistoryLength];
        for (int i = 0; i < HistoryLength; i++)
            result[i] = ring[(_historyPos + i) % HistoryLength];
        return result;
    }
}
