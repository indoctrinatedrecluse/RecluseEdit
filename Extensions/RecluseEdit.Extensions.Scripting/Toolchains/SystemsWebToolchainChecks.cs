using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Scripting.Toolchains;

public class TrunkToolchainCheck : IToolchainCheck
{
    public string ToolName => "Trunk (Rust Wasm)";
    public string Command => "trunk";
    public string? RequiredVersion => ">= 0.19.0";
    public string? InstallHelp => "Install Trunk via 'cargo install --locked trunk' for building and serving Rust Wasm web applications.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainProcessHelper.ExecuteCommandAsync("trunk", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Trunk was not detected on system PATH. Install it to build Rust Wasm web apps.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"trunk\s+v?(\d+\.\d+(\.\d+)?)", RegexOptions.IgnoreCase);
        var version = match.Success ? match.Groups[1].Value : output;

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = "Trunk build tool is available.",
            Path = path,
            InstallHelp = InstallHelp
        };
    }
}

public class WasmPackToolchainCheck : IToolchainCheck
{
    public string ToolName => "wasm-pack";
    public string Command => "wasm-pack";
    public string? RequiredVersion => ">= 0.12.0";
    public string? InstallHelp => "Install wasm-pack via 'cargo install wasm-pack' to build and package Rust WebAssembly modules.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainProcessHelper.ExecuteCommandAsync("wasm-pack", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "wasm-pack was not detected on system PATH.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"wasm-pack\s+v?(\d+\.\d+(\.\d+)?)", RegexOptions.IgnoreCase);
        var version = match.Success ? match.Groups[1].Value : output;

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = "wasm-pack Rust Wasm packaging tool is available.",
            Path = path,
            InstallHelp = InstallHelp
        };
    }
}

public class JavaToolchainCheck : IToolchainCheck
{
    public string ToolName => "Java Development Kit (javac)";
    public string Command => "javac";
    public string? RequiredVersion => ">= 17.0.0";
    public string? InstallHelp => "Install OpenJDK (Temurin, Corretto, or Oracle JDK) from https://adoptium.net/ and ensure javac is on PATH.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainProcessHelper.ExecuteCommandAsync("javac", "-version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Java compiler (javac) was not detected on system PATH.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"javac\s+v?(\d+(\.\d+)*)", RegexOptions.IgnoreCase);
        var version = match.Success ? match.Groups[1].Value : output;

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = "Java Development Kit compiler is available.",
            Path = path,
            InstallHelp = InstallHelp
        };
    }
}

public class MavenToolchainCheck : IToolchainCheck
{
    public string ToolName => "Apache Maven (mvn)";
    public string Command => "mvn";
    public string? RequiredVersion => ">= 3.8.0";
    public string? InstallHelp => "Install Apache Maven from https://maven.apache.org/ or via package manager (choco install maven / brew install maven).";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainProcessHelper.ExecuteCommandAsync("mvn", "-version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Apache Maven was not detected on system PATH.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"Apache\s+Maven\s+v?(\d+\.\d+(\.\d+)?)", RegexOptions.IgnoreCase);
        var version = match.Success ? match.Groups[1].Value : "Detected";

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = "Apache Maven build tool is available.",
            Path = path,
            InstallHelp = InstallHelp
        };
    }
}

public class GradleToolchainCheck : IToolchainCheck
{
    public string ToolName => "Gradle";
    public string Command => "gradle";
    public string? RequiredVersion => ">= 8.0.0";
    public string? InstallHelp => "Install Gradle from https://gradle.org/ or use the ./gradlew wrapper included in your project.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainProcessHelper.ExecuteCommandAsync("gradle", "-version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Gradle was not detected on system PATH.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"Gradle\s+v?(\d+\.\d+(\.\d+)?)", RegexOptions.IgnoreCase);
        var version = match.Success ? match.Groups[1].Value : "Detected";

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = "Gradle build tool is available.",
            Path = path,
            InstallHelp = InstallHelp
        };
    }
}

public class ElixirToolchainCheck : IToolchainCheck
{
    public string ToolName => "Elixir / Mix";
    public string Command => "elixir";
    public string? RequiredVersion => ">= 1.14.0";
    public string? InstallHelp => "Install Elixir and Erlang/OTP from https://elixir-lang.org/install.html.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainProcessHelper.ExecuteCommandAsync("elixir", "-v", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Elixir runtime was not detected on system PATH.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"Elixir\s+v?(\d+\.\d+(\.\d+)?)", RegexOptions.IgnoreCase);
        var version = match.Success ? match.Groups[1].Value : "Detected";

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = "Elixir and Mix toolchain is available.",
            Path = path,
            InstallHelp = InstallHelp
        };
    }
}

