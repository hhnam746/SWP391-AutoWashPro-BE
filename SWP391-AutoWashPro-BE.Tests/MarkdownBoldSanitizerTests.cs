using SWP391_AutoWashPro_BE.Service.AiService;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class MarkdownBoldSanitizerTests
{
    [Fact]
    public void RemoveBoldMarkers_SingleBoldSegment_RemovesBoldSyntax()
    {
        var result = MarkdownBoldSanitizer.RemoveBoldMarkers("**Hao Thien**");

        Assert.Equal("Hao Thien", result);
    }

    [Fact]
    public void RemoveBoldMarkers_BoldPhoneNumber_KeepsInnerText()
    {
        var result = MarkdownBoldSanitizer.RemoveBoldMarkers("so dien thoai la **0388777934**");

        Assert.Equal("so dien thoai la 0388777934", result);
    }

    [Fact]
    public void RemoveBoldMarkers_MultipleBoldSegments_RemovesAllPairs()
    {
        var result = MarkdownBoldSanitizer.RemoveBoldMarkers("Chao **Hao Thien**, so cua ban la **0388777934**.");

        Assert.Equal("Chao Hao Thien, so cua ban la 0388777934.", result);
    }

    [Fact]
    public void RemoveBoldMarkers_NoBoldMarkers_ReturnsOriginalText()
    {
        const string content = "Minh co the giup gi them cho ban khong?";

        var result = MarkdownBoldSanitizer.RemoveBoldMarkers(content);

        Assert.Equal(content, result);
    }

    [Fact]
    public void RemoveBoldMarkers_OtherMarkdown_RemainsUnchanged()
    {
        const string content = "*xin chao* _profile_ # title";

        var result = MarkdownBoldSanitizer.RemoveBoldMarkers(content);

        Assert.Equal(content, result);
    }
}
