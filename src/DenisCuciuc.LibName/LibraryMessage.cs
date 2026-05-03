namespace DenisCuciuc.LibName;

/// <summary>
/// Provides a minimal public API that template consumers can replace with their own domain code.
/// </summary>
public static class LibraryMessage
{
    /// <summary>
    /// Creates a welcome message for the provided subject.
    /// </summary>
    /// <param name="subject">The subject to include in the message.</param>
    /// <returns>A trimmed welcome message.</returns>
    public static string Create(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        return $"Welcome to {subject.Trim()}.";
    }
}
