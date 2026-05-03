using Xunit;

namespace DenisCuciuc.LibName.Tests;

/// <summary>
/// Covers the starter public API shipped with the template.
/// </summary>
public sealed class LibraryMessageTests
{
    /// <summary>
    /// Verifies that the starter API trims input before formatting the message.
    /// </summary>
    [Fact]
    public void Create_ReturnsTrimmedWelcomeMessage()
    {
        var result = LibraryMessage.Create("  platform team  ");

        Assert.Equal("Welcome to platform team.", result);
    }

    /// <summary>
    /// Verifies that the starter API rejects blank subjects.
    /// </summary>
    [Fact]
    public void Create_ThrowsForMissingSubject()
    {
        Assert.Throws<ArgumentException>(() => LibraryMessage.Create(" "));
    }
}
