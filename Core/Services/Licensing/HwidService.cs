using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace RecluseEdit.Core.Services.Licensing;

/// <summary>
/// Generates a stable, anonymised Hardware ID (HWID) for node-locking.
/// Uses Windows Registry MachineGuid + hostname + OS version, hashed via SHA-256.
/// Format: HWID-XXXX-XXXX-XXXX (12 hex chars split into three groups of four).
/// </summary>
public static class HwidService
{
    private static string? _cachedHwid;

    public static string GetHwid()
    {
        if (_cachedHwid is not null)
            return _cachedHwid;

        var raw = BuildRawFingerprint();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var hex = Convert.ToHexString(bytes)[..12]; // first 12 hex chars = 48 bits
        _cachedHwid = $"HWID-{hex[..4]}-{hex[4..8]}-{hex[8..12]}";
        return _cachedHwid;
    }

    private static string BuildRawFingerprint()
    {
        var machineGuid = TryReadMachineGuid() ?? "unknown-guid";
        var hostname = Environment.MachineName;
        var osVersion = Environment.OSVersion.VersionString;
        return $"{machineGuid}-{hostname}-{osVersion}";
    }

    private static string? TryReadMachineGuid()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Cryptography", writable: false);
            return key?.GetValue("MachineGuid") as string;
        }
        catch
        {
            return null;
        }
    }
}
