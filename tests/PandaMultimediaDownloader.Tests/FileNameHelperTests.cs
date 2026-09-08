using PandaMultimediaDownloader.Services;

namespace PandaMultimediaDownloader.Tests;

public class FileNameHelperTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=10s", "youtube_dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "youtube_dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/abc123XYZ", "youtube_abc123XYZ")]
    [InlineData("https://m.youtube.com/watch?v=xyz", "youtube_xyz")]
    [InlineData("https://www.instagram.com/reel/C9AbCdEfGh/?igsh=1", "instagram_C9AbCdEfGh")]
    [InlineData("https://www.instagram.com/p/Cxyz123/", "instagram_Cxyz123")]
    [InlineData("https://www.facebook.com/watch/?v=123456789", "facebook_123456789")]
    [InlineData("https://www.facebook.com/somepage/videos/987654321/", "facebook_987654321")]
    [InlineData("https://fb.watch/abcDEF/", "facebook_abcDEF")]
    [InlineData("https://vimeo.com/123456", "vimeo_123456")]
    public void FromUrl_DerivesProviderAndId(string url, string expected)
    {
        Assert.Equal(expected, FileNameHelper.FromUrl(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData(null)]
    public void FromUrl_InvalidInput_ReturnsFallback(string? url)
    {
        Assert.Equal("media", FileNameHelper.FromUrl(url));
    }

    [Fact]
    public void FromUrl_HostWithoutId_ReturnsProviderOnly()
    {
        Assert.Equal("youtube", FileNameHelper.FromUrl("https://www.youtube.com/"));
    }

    [Theory]
    [InlineData("Hello: World / Test?", "Hello_ World _ Test")]
    [InlineData("  spaced   out  ", "spaced out")]
    [InlineData("100% legit", "100_ legit")]
    [InlineData("trailing dots...", "trailing dots")]
    [InlineData("CON", "media")]
    [InlineData("   ", "media")]
    public void Sanitize_RemovesInvalidCharacters(string input, string expected)
    {
        Assert.Equal(expected, FileNameHelper.Sanitize(input));
    }

    [Fact]
    public void Sanitize_TruncatesLongNames()
    {
        var result = FileNameHelper.Sanitize(new string('a', 500));
        Assert.Equal(FileNameHelper.MaxLength, result.Length);
    }

    [Fact]
    public void BuildBaseName_PrefersTitle()
    {
        Assert.Equal("My Video", FileNameHelper.BuildBaseName("My Video", "https://youtu.be/abc"));
    }

    [Fact]
    public void BuildBaseName_FallsBackToUrl_WhenTitleEmpty()
    {
        Assert.Equal("youtube_abc", FileNameHelper.BuildBaseName(null, "https://youtu.be/abc"));
        Assert.Equal("youtube_abc", FileNameHelper.BuildBaseName("   ", "https://youtu.be/abc"));
    }

    [Fact]
    public void UniquePath_AppendsCounterWhenFileExists()
    {
        var dir = Path.Combine(Path.GetTempPath(), "panda-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Assert.Equal(Path.Combine(dir, "clip.mp4"), FileNameHelper.UniquePath(dir, "clip", ".mp4"));

            File.WriteAllText(Path.Combine(dir, "clip.mp4"), string.Empty);
            Assert.Equal(Path.Combine(dir, "clip_2.mp4"), FileNameHelper.UniquePath(dir, "clip", ".mp4"));

            File.WriteAllText(Path.Combine(dir, "clip_2.mp4"), string.Empty);
            Assert.Equal(Path.Combine(dir, "clip_3.mp4"), FileNameHelper.UniquePath(dir, "clip", ".mp4"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
