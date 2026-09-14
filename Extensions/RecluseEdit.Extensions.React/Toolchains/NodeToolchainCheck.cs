using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Toolchains;

namespace RecluseEdit.Extensions.React.Toolchains;

/// <summary>
/// Verifies the presence of the Node.js JavaScript runtime on the system PATH.
/// </summary>
public class NodeJsToolchainCheck : BaseToolchainCheck
{
    public override string ToolName => "Node.js";
    public override string Command => "node";
    public override string? RequiredVersion => ">= 18.0.0";
    public override string? InstallHelp => "Download Node.js from https://nodejs.org or install via winget: 'winget install OpenJS.NodeJS'";
    public override string? SdkId => KnownSdk.NodeJs;
    public override string? VersionRegex => @"v?(\d+\.\d+\.\d+)";
    public override int TimeoutMs => 6000;
}

/// <summary>
/// Verifies the presence of npm (Node Package Manager) on the system PATH.
/// </summary>
public class NpmToolchainCheck : BaseToolchainCheck
{
    public override string ToolName => "npm";
    public override string Command => "npm";
    public override string? RequiredVersion => ">= 9.0.0";
    public override string? InstallHelp => "npm is bundled with Node.js. Install Node.js from https://nodejs.org";
    public override string? SdkId => KnownSdk.NodeJs;
    public override int TimeoutMs => 6000;
}

/// <summary>
/// Verifies the presence of the TypeScript compiler (tsc) on the system PATH.
/// </summary>
public class TypeScriptToolchainCheck : BaseToolchainCheck
{
    public override string ToolName => "TypeScript (tsc)";
    public override string Command => "tsc";
    public override string? RequiredVersion => ">= 5.0.0";
    public override string? InstallHelp => "Install globally via: 'npm install -g typescript' or within project: 'npm install --save-dev typescript'";
    public override ToolchainStatus StatusOnMissing => ToolchainStatus.Warning;
    public override string DescriptionOnMissing => "TypeScript compiler (tsc) was not detected. Type checking will be limited to editor diagnostics.";
    public override string? VersionRegex => @"Version\s+([0-9\.]+)";
    public override int TimeoutMs => 6000;
}
