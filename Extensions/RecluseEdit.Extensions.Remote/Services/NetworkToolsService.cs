using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RecluseEdit.Extensions.Remote.Services;

public class PingReport
{
    public string Host { get; set; } = string.Empty;
    public string ResolvedAddress { get; set; } = string.Empty;
    public int SentCount { get; set; }
    public int ReceivedCount { get; set; }
    public int LostCount => SentCount - ReceivedCount;
    public double PacketLossPercent => SentCount > 0 ? (LostCount / (double)SentCount) * 100.0 : 0.0;
    public long MinRoundtripTimeMs { get; set; } = long.MaxValue;
    public long MaxRoundtripTimeMs { get; set; } = 0;
    public double AvgRoundtripTimeMs { get; set; }
    public List<string> Details { get; set; } = [];
}

public class PortScanResult
{
    public int Port { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public long LatencyMs { get; set; }
}

public class DnsLookupReport
{
    public string Hostname { get; set; } = string.Empty;
    public List<string> IPv4Addresses { get; set; } = [];
    public List<string> IPv6Addresses { get; set; } = [];
    public List<string> Aliases { get; set; } = [];
}

/// <summary>
/// Network utility toolkit offering Ping, concurrent Port Scanning, and DNS resolution (MobaXterm style).
/// </summary>
public class NetworkToolsService
{
    public static readonly Dictionary<int, string> CommonServices = new()
    {
        { 21, "FTP" },
        { 22, "SSH" },
        { 23, "Telnet" },
        { 25, "SMTP" },
        { 53, "DNS" },
        { 80, "HTTP" },
        { 110, "POP3" },
        { 143, "IMAP" },
        { 443, "HTTPS" },
        { 3000, "Node / React" },
        { 3306, "MySQL" },
        { 3389, "RDP" },
        { 5432, "PostgreSQL" },
        { 6379, "Redis" },
        { 8080, "HTTP-Alt" },
        { 8443, "HTTPS-Alt" },
        { 27017, "MongoDB" }
    };

    public async Task<PingReport> PingHostAsync(string host, int timeoutMs = 1500, int count = 4)
    {
        var report = new PingReport { Host = host, SentCount = count };
        using var ping = new Ping();

        long totalTime = 0;

        for (int i = 0; i < count; i++)
        {
            try
            {
                var reply = await ping.SendPingAsync(host, timeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    report.ReceivedCount++;
                    report.ResolvedAddress = reply.Address.ToString();
                    totalTime += reply.RoundtripTime;
                    if (reply.RoundtripTime < report.MinRoundtripTimeMs) report.MinRoundtripTimeMs = reply.RoundtripTime;
                    if (reply.RoundtripTime > report.MaxRoundtripTimeMs) report.MaxRoundtripTimeMs = reply.RoundtripTime;
                    report.Details.Add($"Reply from {reply.Address}: bytes={reply.Buffer.Length} time={reply.RoundtripTime}ms TTL={reply.Options?.Ttl ?? 0}");
                }
                else
                {
                    report.Details.Add($"Request timed out (status: {reply.Status}).");
                }
            }
            catch (Exception ex)
            {
                report.Details.Add($"Ping error: {ex.Message}");
            }

            if (i < count - 1)
            {
                await Task.Delay(200);
            }
        }

        if (report.ReceivedCount > 0)
        {
            report.AvgRoundtripTimeMs = totalTime / (double)report.ReceivedCount;
        }
        else
        {
            report.MinRoundtripTimeMs = 0;
        }

        return report;
    }

    public Task<IReadOnlyList<PortScanResult>> ScanCommonPortsAsync(string host, int timeoutMs = 800) =>
        ScanPortsAsync(host, CommonServices.Keys.ToArray(), timeoutMs);

    public async Task<IReadOnlyList<PortScanResult>> ScanPortsAsync(string host, int[]? targetPorts = null, int timeoutMs = 800)
    {
        var portsToScan = targetPorts ?? CommonServices.Keys.ToArray();
        var results = new List<PortScanResult>();

        var tasks = portsToScan.Select(async port =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var isOpen = false;

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));

                if (completed == connectTask && client.Connected)
                {
                    isOpen = true;
                }
            }
            catch
            {
                isOpen = false;
            }
            finally
            {
                sw.Stop();
            }

            CommonServices.TryGetValue(port, out var serviceName);

            return new PortScanResult
            {
                Port = port,
                ServiceName = serviceName ?? "Custom",
                IsOpen = isOpen,
                LatencyMs = isOpen ? sw.ElapsedMilliseconds : timeoutMs
            };
        });

        var completedResults = await Task.WhenAll(tasks);
        return completedResults.OrderBy(r => r.Port).ToList().AsReadOnly();
    }

    public async Task<DnsLookupReport> ResolveDnsAsync(string hostname)
    {
        var report = new DnsLookupReport { Hostname = hostname };

        try
        {
            var entry = await Dns.GetHostEntryAsync(hostname);
            report.Hostname = entry.HostName;

            foreach (var ip in entry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    report.IPv4Addresses.Add(ip.ToString());
                }
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    report.IPv6Addresses.Add(ip.ToString());
                }
            }

            if (IPAddress.TryParse(hostname, out var parsedIp))
            {
                if (parsedIp.AddressFamily == AddressFamily.InterNetwork && !report.IPv4Addresses.Contains(parsedIp.ToString()))
                {
                    report.IPv4Addresses.Insert(0, parsedIp.ToString());
                }
                else if (parsedIp.AddressFamily == AddressFamily.InterNetworkV6 && !report.IPv6Addresses.Contains(parsedIp.ToString()))
                {
                    report.IPv6Addresses.Insert(0, parsedIp.ToString());
                }
            }

            report.Aliases.AddRange(entry.Aliases);
        }
        catch (Exception ex)
        {
            report.Aliases.Add($"Error: {ex.Message}");
        }

        return report;
    }
}
