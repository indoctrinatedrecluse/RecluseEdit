namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Interface for AI chat side panels capable of receiving prompts and executing code reviews.
/// </summary>
public interface IAiChatView
{
    /// <summary>
    /// Submits a user prompt into the active chat session.
    /// </summary>
    void SubmitPrompt(string prompt);

    /// <summary>
    /// Starts a structured AI Code Review of the specified source code.
    /// </summary>
    void StartCodeReview(string code, string? fileName = null);
}

