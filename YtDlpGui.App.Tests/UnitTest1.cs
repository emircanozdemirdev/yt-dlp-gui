using System.Text.Json;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.Tests;

public class UnitTest1
{
    [Fact]
    public void ProgressParser_ParsesExpectedValues()
    {
        var parser = new ProgressParser();
        var line = "[download]  45.5% of 100.00MiB at 2.21MiB/s ETA 00:13";

        var result = parser.Parse(line);

        Assert.NotNull(result);
        Assert.Equal(45.5, result!.Percent!.Value, 1);
        Assert.Equal("2.21MiB/s", result.Speed);
        Assert.Equal("00:13", result.Eta);
    }

    [Fact]
    public void ProgressParser_ReturnsNullForUnrelatedLine()
    {
        var parser = new ProgressParser();
        var line = "[info] Extracting URL: https://example.com";

        var result = parser.Parse(line);

        Assert.Null(result);
    }

    [Fact]
    public void YtDlpJsonReader_GetDouble_ReturnsNull_WhenPropertyIsNull()
    {
        using var doc = JsonDocument.Parse("""{"duration":null}""");

        var value = YtDlpJsonReader.GetDouble(doc.RootElement, "duration");

        Assert.Null(value);
    }

    [Fact]
    public void YtDlpJsonReader_GetFileSizeBytes_HandlesNullFilesize()
    {
        using var doc = JsonDocument.Parse("""{"filesize":null,"filesize_approx":1024}""");

        var bytes = YtDlpJsonReader.GetFileSizeBytes(doc.RootElement);

        Assert.Equal(1024, bytes);
    }

    [Fact]
    public void YtDlpJsonReader_FormatBitrateK_ReturnsDash_WhenTbrIsNull()
    {
        using var doc = JsonDocument.Parse("""{"tbr":null}""");

        var text = YtDlpJsonReader.FormatBitrateK(doc.RootElement);

        Assert.Equal("-", text);
    }

    [Fact]
    public void YtDlpJsonReader_GetInt32_ReturnsNull_WhenPropertyIsNull()
    {
        using var doc = JsonDocument.Parse("""{"height":null}""");

        var value = YtDlpJsonReader.GetInt32(doc.RootElement, "height");

        Assert.Null(value);
    }

    [Fact]
    public void FormatListSorter_OrdersVideoThenAudio_HighToLow()
    {
        var formats = new List<FormatOption>
        {
            new()
            {
                FormatId = "a1",
                IsAudioOnly = true,
                SortHeight = 0,
                SortBitrateKbps = 64
            },
            new()
            {
                FormatId = "v1",
                IsAudioOnly = false,
                SortHeight = 720,
                SortBitrateKbps = 2000
            },
            new()
            {
                FormatId = "v2",
                IsAudioOnly = false,
                SortHeight = 1080,
                SortBitrateKbps = 5000
            },
            new()
            {
                FormatId = "a2",
                IsAudioOnly = true,
                SortHeight = 0,
                SortBitrateKbps = 128
            }
        };

        var ordered = FormatListSorter.OrderHighToLow(formats);

        Assert.Equal("v2", ordered[0].FormatId);
        Assert.Equal("v1", ordered[1].FormatId);
        Assert.Equal("a2", ordered[2].FormatId);
        Assert.Equal("a1", ordered[3].FormatId);
    }
}
