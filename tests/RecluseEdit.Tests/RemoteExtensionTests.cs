using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Extensions.Remote;
using RecluseEdit.Extensions.Remote.Models;
using RecluseEdit.Extensions.Remote.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class RemoteExtensionTests
{
    private string _tempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "RecluseEdit_RemoteTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup failure
        }
    }

    [TestMethod]
    public void RemoteExtension_Metadata_IsCorrect()
    {
        var ext = new RemoteExtension();

        Assert.AreEqual("recluse.remote", ext.Id);
        Assert.AreEqual("Remote & SSH Explorer", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        Assert.IsFalse(string.IsNullOrWhiteSpace(ext.Description));
    }

    [TestMethod]
    public void RemoteSidePanelProvider_Properties_AreCorrect()
    {
        var provider = new RemoteSidePanelProvider();

        Assert.AreEqual("recluse.remote", provider.Id);
        Assert.AreEqual("Remote & SSH", provider.Title);
        Assert.AreEqual("🌐", provider.Icon);
    }

    [TestMethod]
    public void SshSessionProfile_Properties_And_Uri_Generation()
    {
        var profile = new SshSessionProfile
        {
            Name = "Production Web",
            Host = "10.0.0.50",
            Port = 2222,
            Username = "deploy"
        };

        Assert.AreEqual("deploy@10.0.0.50:2222", profile.QuickUri);
        Assert.AreEqual("Production Web (deploy@10.0.0.50:2222)", profile.DisplayTitle);
        Assert.AreEqual(SshAuthType.Password, profile.AuthType);
    }

    [TestMethod]
    public void SshProfileStore_Save_Load_Delete_Profiles()
    {
        var storePath = Path.Combine(_tempDir, "remote_sessions.json");
        var store = new SshProfileStore(storePath);

        var testProfile = new SshSessionProfile
        {
            Name = "Test Server",
            Host = "192.168.1.50",
            Port = 22,
            Username = "admin",
            Group = "Testing"
        };

        store.SaveProfile(testProfile);

        // Reload fresh from disk
        var reloadedStore = new SshProfileStore(storePath);
        var profiles = reloadedStore.GetProfiles();

        var found = profiles.FirstOrDefault(p => p.Id == testProfile.Id);
        Assert.IsNotNull(found);
        Assert.AreEqual("Test Server", found.Name);
        Assert.AreEqual("192.168.1.50", found.Host);
        Assert.AreEqual("admin", found.Username);

        // Delete profile
        var deleted = reloadedStore.DeleteProfile(testProfile.Id);
        Assert.IsTrue(deleted);

        var reloadedAfterDelete = new SshProfileStore(storePath);
        Assert.IsFalse(reloadedAfterDelete.GetProfiles().Any(p => p.Id == testProfile.Id));
    }

    [TestMethod]
    public void SshProfileStore_Save_Load_Delete_Tunnels()
    {
        var storePath = Path.Combine(_tempDir, "remote_tunnels.json");
        var store = new SshProfileStore(storePath);

        var tunnel = new SshTunnelConfig
        {
            Name = "MySQL Tunnel",
            SshHost = "remote.host",
            SshPort = 22,
            SshUser = "ubuntu",
            LocalPort = 3307,
            RemoteHost = "127.0.0.1",
            RemotePort = 3306
        };

        store.SaveTunnel(tunnel);

        var reloaded = new SshProfileStore(storePath);
        var found = reloaded.GetTunnels().FirstOrDefault(t => t.Id == tunnel.Id);
        Assert.IsNotNull(found);
        Assert.AreEqual("MySQL Tunnel", found.Name);
        Assert.AreEqual(3307u, found.LocalPort);
        Assert.AreEqual(3306u, found.RemotePort);

        var deleted = reloaded.DeleteTunnel(tunnel.Id);
        Assert.IsTrue(deleted);
    }

    [TestMethod]
    public void SshTunnelConfig_Description_Format()
    {
        var tunnel = new SshTunnelConfig
        {
            Name = "Redis Forward",
            SshHost = "redis.server",
            SshPort = 22,
            SshUser = "app",
            LocalPort = 6380,
            RemoteHost = "10.0.0.10",
            RemotePort = 6379
        };

        Assert.AreEqual("127.0.0.1:6380 ➔ 10.0.0.10:6379 (via app@redis.server:22)", tunnel.Description);
    }

    [TestMethod]
    public void SshKeyGenService_GenerateRsaKey_ProducesValidKeys()
    {
        var keyPair = SshKeyGenService.GenerateRsaKey(2048, "test@recluseedit");

        Assert.IsNotNull(keyPair);
        Assert.AreEqual("RSA", keyPair.KeyType);
        Assert.AreEqual(2048, keyPair.KeySize);

        // OpenSSH public key starts with "ssh-rsa " and ends with comment
        StringAssert.StartsWith(keyPair.PublicKeyOpenSsh, "ssh-rsa ");
        StringAssert.EndsWith(keyPair.PublicKeyOpenSsh, "test@recluseedit");

        // Private key PEM format contains PKCS#8 or PKCS#1 headers
        Assert.Contains("BEGIN", keyPair.PrivateKeyPem);
        Assert.Contains("PRIVATE KEY", keyPair.PrivateKeyPem);
        Assert.Contains("END", keyPair.PrivateKeyPem);
    }

    [TestMethod]
    public async Task NetworkToolsService_Ping_Localhost_Completes()
    {
        var tools = new NetworkToolsService();
        var report = await tools.PingHostAsync("127.0.0.1", count: 1, timeoutMs: 1000);

        Assert.IsNotNull(report);
        Assert.AreEqual("127.0.0.1", report.Host);
        Assert.AreEqual(1, report.SentCount);
        Assert.IsLessThanOrEqualTo(report.ReceivedCount, 1);
    }

    [TestMethod]
    public async Task NetworkToolsService_DnsLookup_Localhost_Completes()
    {
        var tools = new NetworkToolsService();
        var report = await tools.ResolveDnsAsync("127.0.0.1");

        Assert.IsNotNull(report);
        Assert.IsFalse(string.IsNullOrWhiteSpace(report.Hostname));
        Assert.Contains("127.0.0.1", report.IPv4Addresses);
    }

    [TestMethod]
    public async Task NetworkToolsService_ScanPorts_ReturnsTargetPorts()
    {
        var tools = new NetworkToolsService();
        var ports = new[] { 80, 22 };
        var results = await tools.ScanPortsAsync("127.0.0.1", ports, timeoutMs: 200);

        Assert.IsNotNull(results);
        Assert.HasCount(2, results);
        Assert.IsTrue(results.Any(r => r.Port == 80));
        Assert.IsTrue(results.Any(r => r.Port == 22));
    }
}

