using System.Configuration;
using System.Data;
using System.Windows;

namespace RecluseEdit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static bool _isHandlingException = false;
        private static DateTime _lastErrorDialogTime = DateTime.MinValue;

        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                // Prevent crash by marking as handled
                e.Handled = true;

                // Re-entrancy guard: if an error occurs while showing an error or during handling, ignore
                if (_isHandlingException)
                {
                    return;
                }

                var ex = e.Exception;
                var exMsg = ex?.Message ?? string.Empty;
                var stackTrace = ex?.StackTrace ?? string.Empty;

                // Detect if this is a syntax highlighting or rendering exception from AvalonEdit
                bool isHighlightingOrRenderError =
                    (ex is InvalidOperationException && exMsg.Contains("highlighting", StringComparison.OrdinalIgnoreCase)) ||
                    (ex?.GetType().Name.Contains("Highlighting", StringComparison.OrdinalIgnoreCase) == true) ||
                    stackTrace.Contains("HighlightLine", StringComparison.OrdinalIgnoreCase) ||
                    stackTrace.Contains("DocumentHighlighter", StringComparison.OrdinalIgnoreCase) ||
                    stackTrace.Contains("HighlightingEngine", StringComparison.OrdinalIgnoreCase) ||
                    stackTrace.Contains("AvalonEdit.Rendering", StringComparison.OrdinalIgnoreCase);

                if (isHighlightingOrRenderError)
                {
                    // Fallback gracefully: disable syntax highlighting on the active editor so rendering resumes smoothly as plain text
                    try
                    {
                        if (Current?.MainWindow is MainWindow mainWin)
                        {
                            mainWin.EditorHost.Editor.SyntaxHighlighting = null;
                            mainWin.StatusMessage.Text = "Syntax highlighting disabled due to an error in the language definition.";
                        }
                    }
                    catch { }

                    System.Diagnostics.Debug.WriteLine($"[RecluseEdit] Syntax/Render error handled gracefully: {exMsg}");
                    return;
                }

                // Rate-limiting: at most one modal dialog every 2 seconds
                if (DateTime.UtcNow - _lastErrorDialogTime < TimeSpan.FromSeconds(2))
                {
                    return;
                }

                _isHandlingException = true;
                _lastErrorDialogTime = DateTime.UtcNow;

                try
                {
                    MessageBox.Show(
                        $"An unexpected error occurred:\n{exMsg}",
                        "RecluseEdit Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch
                {
                    // Ignore secondary UI failures
                }
                finally
                {
                    _isHandlingException = false;
                }
            };
        }
    }
}
