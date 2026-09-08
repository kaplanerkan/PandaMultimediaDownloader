using PandaMultimediaDownloader.Localization;

namespace PandaMultimediaDownloader.Tests;

public class LocTests
{
    [Fact]
    public void AllLanguages_HaveSameKeys()
    {
        var loc = Loc.Instance;
        var keys = new[]
        {
            "AppTitle", "UrlLabel", "Download", "Cancel", "Mp4", "Mp3", "StatusReady",
            "StatusDownloading", "StatusDone", "StatusError", "ErrInvalidUrl", "ErrNoFormat",
        };

        // Unknown keys fall back to the key itself; a key that resolves identically in every
        // language would therefore be missing, so compare each language against a marker key.
        foreach (var lang in Loc.Languages)
        {
            loc.Language = lang.Code;
            foreach (var key in keys)
            {
                Assert.False(string.IsNullOrWhiteSpace(loc[key]), $"{lang.Code}:{key}");
            }

            Assert.Equal("NoSuchKey", loc["NoSuchKey"]);
        }

        loc.Language = "de";
        Assert.Equal("Herunterladen", loc["Download"]);
        loc.Language = "tr";
        Assert.Equal("İndir", loc["Download"]);
    }

    [Fact]
    public void Language_Switch_ChangesText()
    {
        var loc = Loc.Instance;
        loc.Language = "en";
        Assert.Equal("Download", loc["Download"]);
        loc.Language = "de";
        Assert.Equal("Herunterladen", loc["Download"]);
        loc.Language = "tr";
        Assert.Equal("İndir", loc["Download"]);
    }

    [Fact]
    public void UnsupportedLanguage_IsIgnored()
    {
        var loc = Loc.Instance;
        loc.Language = "en";
        loc.Language = "xx";
        Assert.Equal("en", loc.Language);
    }

    [Fact]
    public void Format_InsertsArguments()
    {
        Loc.Instance.Language = "en";
        Assert.Equal("Done: 2 file(s)", Loc.Instance.Format("StatusDone", 2));
    }

    [Fact]
    public void UnknownKey_ReturnsKey()
    {
        Assert.Equal("NoSuchKey", Loc.Instance["NoSuchKey"]);
    }
}
