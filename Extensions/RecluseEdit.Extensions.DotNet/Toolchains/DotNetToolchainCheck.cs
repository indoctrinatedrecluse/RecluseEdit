using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Toolchains;

namespace RecluseEdit.Extensions.DotNet.Toolchains;

public class DotNetToolchainCheck : BaseToolchainCheck
{
    public override string ToolName => ".NET SDK & CLI (dotnet)";
    public override string Command => "dotnet";
    public override string? RequiredVersion => ">= 8.0.0";
    public override string? InstallHelp => "Install .NET SDK from https://dotnet.microsoft.com/download or via winget: 'winget install Microsoft.DotNet.SDK.10'";
    public override string? SdkId => KnownSdk.DotNet;
    public override ToolchainStatus StatusOnMissing => ToolchainStatus.Warning;

    public override string DescriptionOnSuccess(string? version) =>
        ".NET SDK is available for compiling, running, and publishing applications.";
}
