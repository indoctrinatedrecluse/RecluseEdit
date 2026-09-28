using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services.Licensing;

namespace RecluseEdit.Tests;

/// <summary>Unit tests for LicenseService and related licensing helpers.</summary>
[TestClass]
public class LicenseServiceTests
{
    // ─── Tier detection ───────────────────────────────────────────────────────

    [TestMethod]
    public void DetectTier_AdmPrefix_ReturnsAdmin()
    {
        var tier = LicenseService.DetectLicenseTier("ADM-1234-ABCD-5678");
        Assert.AreEqual(LicenseTier.Admin, tier);
    }

    [TestMethod]
    public void DetectTier_UserPrefix_ReturnsUser()
    {
        var tier = LicenseService.DetectLicenseTier("USER-AAAA-BBBB-CCCC");
        Assert.AreEqual(LicenseTier.User, tier);
    }

    [TestMethod]
    public void DetectTier_DevPrefix_ReturnsDeveloper()
    {
        var tier = LicenseService.DetectLicenseTier("DEV-1111-2222-3333");
        Assert.AreEqual(LicenseTier.Developer, tier);
    }

    [TestMethod]
    public void DetectTier_InvalidPrefix_ReturnsUnlicensed()
    {
        var tier = LicenseService.DetectLicenseTier("INVALID-1234-5678-9012");
        Assert.AreEqual(LicenseTier.Unlicensed, tier);
    }

    [TestMethod]
    public void DetectTier_EmptyString_ReturnsUnlicensed()
    {
        var tier = LicenseService.DetectLicenseTier(string.Empty);
        Assert.AreEqual(LicenseTier.Unlicensed, tier);
    }

    // ─── Key format validation ────────────────────────────────────────────────

    [TestMethod]
    public void ValidateKeyFormat_ValidAdmKey_ReturnsTrue()
    {
        Assert.IsTrue(LicenseService.ValidateKeyFormat("ADM-1234-ABCD-5678"));
    }

    [TestMethod]
    public void ValidateKeyFormat_ValidUserKey_ReturnsTrue()
    {
        Assert.IsTrue(LicenseService.ValidateKeyFormat("USER-ABCD-1234-EF01"));
    }

    [TestMethod]
    public void ValidateKeyFormat_ValidDevKey_ReturnsTrue()
    {
        Assert.IsTrue(LicenseService.ValidateKeyFormat("DEV-ZZZZ-YYYY-XXXX"));
    }

    [TestMethod]
    public void ValidateKeyFormat_MissingGroup_ReturnsFalse()
    {
        Assert.IsFalse(LicenseService.ValidateKeyFormat("USER-ABCD-1234"));
    }

    [TestMethod]
    public void ValidateKeyFormat_WrongGroupLength_ReturnsFalse()
    {
        Assert.IsFalse(LicenseService.ValidateKeyFormat("USER-ABC-1234-EF01"));
    }

    [TestMethod]
    public void ValidateKeyFormat_InvalidPrefix_ReturnsFalse()
    {
        Assert.IsFalse(LicenseService.ValidateKeyFormat("FREE-ABCD-1234-EF01"));
    }

    // ─── HWID stability ───────────────────────────────────────────────────────

    [TestMethod]
    public void GetHwid_SameCallTwice_ReturnsSameValue()
    {
        var h1 = HwidService.GetHwid();
        var h2 = HwidService.GetHwid();
        Assert.AreEqual(h1, h2);
    }

    [TestMethod]
    public void GetHwid_MatchesExpectedFormat()
    {
        var hwid = HwidService.GetHwid();
        // Expected: HWID-XXXX-XXXX-XXXX
        Assert.IsTrue(hwid.StartsWith("HWID-"), $"HWID should start with 'HWID-', got: {hwid}");
        var parts = hwid.Split('-');
        Assert.AreEqual(4, parts.Length, "HWID should have 4 dash-separated parts");
        Assert.AreEqual("HWID", parts[0]);
        Assert.AreEqual(4, parts[1].Length);
        Assert.AreEqual(4, parts[2].Length);
        Assert.AreEqual(4, parts[3].Length);
    }

    // ─── LicenseInfo model ────────────────────────────────────────────────────

    [TestMethod]
    public void TrialLicenseInfo_IsValid_ReturnsTrue()
    {
        var info = new LicenseInfo
        {
            Tier          = LicenseTier.Trial,
            Status        = LicenseStatus.Trial,
            LicenseKey    = "TRIAL",
            TrialStartUtc = DateTime.UtcNow
        };
        Assert.IsTrue(info.IsValid);
    }

    [TestMethod]
    public void TrialLicenseInfo_AllExtensionsUnlocked_ReturnsFalse()
    {
        var info = new LicenseInfo
        {
            Tier       = LicenseTier.Trial,
            Status     = LicenseStatus.Trial,
            LicenseKey = "TRIAL"
        };
        Assert.IsFalse(info.AllExtensionsUnlocked,
            "Trial license should NOT unlock premium extensions.");
    }

    [TestMethod]
    public void AdminLicenseInfo_AllExtensionsUnlocked_ReturnsTrue()
    {
        var info = new LicenseInfo
        {
            Tier       = LicenseTier.Admin,
            Status     = LicenseStatus.Active,
            LicenseKey = "ADM-1234-ABCD-5678"
        };
        Assert.IsTrue(info.AllExtensionsUnlocked,
            "Admin license should unlock all extensions.");
    }

    [TestMethod]
    public void UserLicenseInfo_Active_AllExtensionsUnlocked_ReturnsTrue()
    {
        var info = new LicenseInfo
        {
            Tier       = LicenseTier.User,
            Status     = LicenseStatus.Active,
            LicenseKey = "USER-ABCD-1234-EF01"
        };
        Assert.IsTrue(info.AllExtensionsUnlocked);
    }

    [TestMethod]
    public void ExpiredTrialInfo_IsValid_ReturnsFalse()
    {
        var info = new LicenseInfo
        {
            Tier          = LicenseTier.Trial,
            Status        = LicenseStatus.TrialExpired,
            LicenseKey    = "TRIAL",
            TrialStartUtc = DateTime.UtcNow.AddDays(-61)
        };
        Assert.IsFalse(info.IsValid);
    }

    [TestMethod]
    public void TrialDaysRemaining_StartedToday_Returns60()
    {
        var info = new LicenseInfo
        {
            Tier          = LicenseTier.Trial,
            Status        = LicenseStatus.Trial,
            LicenseKey    = "TRIAL",
            TrialStartUtc = DateTime.UtcNow
        };
        Assert.AreEqual(60, info.TrialDaysRemaining);
    }

    [TestMethod]
    public void TrialDaysRemaining_Started50DaysAgo_Returns10()
    {
        var info = new LicenseInfo
        {
            Tier          = LicenseTier.Trial,
            Status        = LicenseStatus.Trial,
            LicenseKey    = "TRIAL",
            TrialStartUtc = DateTime.UtcNow.AddDays(-50)
        };
        Assert.AreEqual(10, info.TrialDaysRemaining);
    }

    [TestMethod]
    public void TrialDaysRemaining_Expired_ReturnsZero()
    {
        var info = new LicenseInfo
        {
            Tier          = LicenseTier.Trial,
            Status        = LicenseStatus.TrialExpired,
            LicenseKey    = "TRIAL",
            TrialStartUtc = DateTime.UtcNow.AddDays(-70)
        };
        Assert.AreEqual(0, info.TrialDaysRemaining);
    }

    // ─── Badge display ────────────────────────────────────────────────────────

    [TestMethod]
    public void AdminLicenseInfo_BadgeLabel_IsADM()
    {
        var info = new LicenseInfo { Tier = LicenseTier.Admin, Status = LicenseStatus.Active, LicenseKey = "ADM-X" };
        Assert.AreEqual("ADM", info.BadgeLabel);
    }

    [TestMethod]
    public void TrialLicenseInfo_BadgeLabel_IsTrial()
    {
        var info = new LicenseInfo { Tier = LicenseTier.Trial, Status = LicenseStatus.Trial, LicenseKey = "TRIAL" };
        Assert.AreEqual("TRIAL", info.BadgeLabel);
    }
}
