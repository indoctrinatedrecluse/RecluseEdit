using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Extensions.AiChat.Services;

namespace RecluseEdit.Extensions.DeepSeek
{
    /// <summary>
    /// Backward-compatibility wrapper for DeepSeekExtension.
    /// </summary>
    public class DeepSeekExtension : global::RecluseEdit.Extensions.AiChat.AiChatExtension { }

    /// <summary>
    /// Backward-compatibility wrapper for DeepSeekSidePanelProvider.
    /// </summary>
    public class DeepSeekSidePanelProvider : global::RecluseEdit.Extensions.AiChat.AiChatSidePanelProvider { }
}

namespace RecluseEdit.Extensions.DeepSeek.Views
{
    /// <summary>
    /// Backward-compatibility wrapper for DeepSeekChatView.
    /// </summary>
    public class DeepSeekChatView : global::RecluseEdit.Extensions.AiChat.Views.AiChatView
    {
        public DeepSeekChatView(
            IWorkspaceContext workspaceContext,
            global::RecluseEdit.Extensions.AiChat.Services.AiChatSettingsService? settingsService = null,
            global::RecluseEdit.Extensions.AiChat.Services.AiChatApiClient? apiClient = null)
            : base(workspaceContext, settingsService, apiClient) { }
    }
}

namespace RecluseEdit.Extensions.DeepSeek.Models
{
    /// <summary>
    /// Backward-compatibility wrapper for DeepSeekSettings.
    /// </summary>
    public class DeepSeekSettings : global::RecluseEdit.Extensions.AiChat.Models.AiChatSettings { }
}

namespace RecluseEdit.Extensions.DeepSeek.Services
{
    /// <summary>
    /// Backward-compatibility wrapper for DeepSeekSettingsService.
    /// </summary>
    public class DeepSeekSettingsService : global::RecluseEdit.Extensions.AiChat.Services.AiChatSettingsService
    {
        public DeepSeekSettingsService(string? customPath = null) : base(customPath) { }
    }

    /// <summary>
    /// Backward-compatibility wrapper for DeepSeekApiClient.
    /// </summary>
    public class DeepSeekApiClient : global::RecluseEdit.Extensions.AiChat.Services.AiChatApiClient
    {
        public DeepSeekApiClient(System.Net.Http.HttpClient? httpClient = null) : base(httpClient) { }
    }

    /// <summary>
    /// Backward-compatibility forwarder for AiProviderRegistry.
    /// </summary>
    public static class AiProviderRegistry
    {
        public static IReadOnlyList<AiProviderDescriptor> Providers =>
            global::RecluseEdit.Extensions.AiChat.Services.AiProviderRegistry.Providers;

        public static AiProviderDescriptor GetProvider(string? providerId) =>
            global::RecluseEdit.Extensions.AiChat.Services.AiProviderRegistry.GetProvider(providerId);

        public static Task<(bool Found, string? Token, string? Info, string? SuggestedModel)> AutoDetectCredentialsAsync(string? providerId) =>
            global::RecluseEdit.Extensions.AiChat.Services.AiProviderRegistry.AutoDetectCredentialsAsync(providerId ?? "");
    }
}

namespace RecluseEdit.Extensions.DeepSeek.Rendering
{
    /// <summary>
    /// Backward-compatibility forwarder for MarkdownBlockRenderer.
    /// </summary>
    public static class MarkdownBlockRenderer
    {
        public static void RenderInto(StackPanel panel, string markdown, Brush textBrush, Brush accentBrush, Brush codeBg, Brush codeBorder) =>
            global::RecluseEdit.Extensions.AiChat.Rendering.MarkdownBlockRenderer.RenderInto(panel, markdown, textBrush, accentBrush, codeBg, codeBorder);

        public static void AppendFormattedInlines(TextBlock textBlock, string text, Brush textBrush) =>
            global::RecluseEdit.Extensions.AiChat.Rendering.MarkdownBlockRenderer.AppendFormattedInlines(textBlock, text, textBrush);
    }
}
