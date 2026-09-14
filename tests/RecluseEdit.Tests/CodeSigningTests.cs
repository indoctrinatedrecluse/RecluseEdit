using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace RecluseEdit.Tests;

[TestClass]
public class CodeSigningTests
{
    private static string GetRepoRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "RecluseEdit.slnx")) ||
                File.Exists(Path.Combine(current, "RecluseEdit.csproj")))
            {
                return current;
            }
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        // Fallback to project root if running from tests folder
        return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    [TestMethod]
    public void CodeSigningScripts_ExistAndAreConfiguredCorrectly()
    {
        var repoRoot = GetRepoRoot();
        var scriptsDir = Path.Combine(repoRoot, "scripts");
        Assert.IsTrue(Directory.Exists(scriptsDir), $"Scripts directory must exist at {scriptsDir}");

        var ensureScript = Path.Combine(scriptsDir, "ensure-ca-cert.ps1");
        var signScript = Path.Combine(scriptsDir, "sign-release.ps1");
        var buildSignScript = Path.Combine(scriptsDir, "build-and-sign.ps1");
        var installCaScript = Path.Combine(scriptsDir, "install-root-ca.ps1");

        Assert.IsTrue(File.Exists(ensureScript), "ensure-ca-cert.ps1 must exist");
        Assert.IsTrue(File.Exists(signScript), "sign-release.ps1 must exist");
        Assert.IsTrue(File.Exists(buildSignScript), "build-and-sign.ps1 must exist");
        Assert.IsTrue(File.Exists(installCaScript), "install-root-ca.ps1 must exist");

        var ensureContent = File.ReadAllText(ensureScript);
        StringAssert.Contains(ensureContent, "indoctrinatedrecluse", "ensure-ca-cert.ps1 must target organization 'indoctrinatedrecluse'");
        StringAssert.Contains(ensureContent, "New-SelfSignedCertificate", "ensure-ca-cert.ps1 must generate certificates");
        StringAssert.Contains(ensureContent, "CodeSigningCert", "ensure-ca-cert.ps1 must create CodeSigningCert");

        var signContent = File.ReadAllText(signScript);
        StringAssert.Contains(signContent, "Set-AuthenticodeSignature", "sign-release.ps1 must use Set-AuthenticodeSignature");
        StringAssert.Contains(signContent, "SHA256", "sign-release.ps1 must default to SHA256");
        StringAssert.Contains(signContent, "indoctrinatedrecluse", "sign-release.ps1 must target organization 'indoctrinatedrecluse'");
    }

    [TestMethod]
    public void RootCertificate_FileExists_AndHasExpectedSubject()
    {
        var repoRoot = GetRepoRoot();
        var certPath = Path.Combine(repoRoot, "certs", "indoctrinatedrecluse-RootCA.cer");
        Assert.IsTrue(File.Exists(certPath), $"Public Root CA certificate must exist at {certPath}");

        using var cert = X509CertificateLoader.LoadCertificateFromFile(certPath);
        Assert.IsNotNull(cert);
        StringAssert.Contains(cert.Subject, "indoctrinatedrecluse", "Subject must contain indoctrinatedrecluse");
        StringAssert.Contains(cert.Subject, "Root CA", "Subject must contain Root CA");
        Assert.IsTrue(cert.NotAfter > DateTime.UtcNow, "Root CA certificate must be valid and not expired");
    }

    [TestMethod]
    public void ReleaseWorkflow_IncludesCodeSigningStep()
    {
        var repoRoot = GetRepoRoot();
        var workflowPath = Path.Combine(repoRoot, ".github", "workflows", "release.yml");
        Assert.IsTrue(File.Exists(workflowPath), "release.yml must exist");

        var workflowContent = File.ReadAllText(workflowPath);
        StringAssert.Contains(workflowContent, "sign-release.ps1", "release.yml must invoke sign-release.ps1");
        StringAssert.Contains(workflowContent, "indoctrinatedrecluse-RootCA.cer", "release.yml must bundle Root CA certificate");
    }

    [TestMethod]
    public void GitIgnore_ProperlyConfigured_ForCertificates()
    {
        var repoRoot = GetRepoRoot();
        var gitignorePath = Path.Combine(repoRoot, ".gitignore");
        Assert.IsTrue(File.Exists(gitignorePath), ".gitignore must exist");

        var content = File.ReadAllText(gitignorePath);
        StringAssert.Contains(content, "*.pfx", ".gitignore must exclude private keys (*.pfx)");
        StringAssert.Contains(content, "!certs/*.cer", ".gitignore must permit public root certificates (!certs/*.cer)");
    }
}
