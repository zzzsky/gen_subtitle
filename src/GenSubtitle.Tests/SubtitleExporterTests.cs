using System;
using System.Collections.Generic;
using GenSubtitle.Core.Models;
using GenSubtitle.Core.Services;
using Xunit;

namespace GenSubtitle.Tests;

public class SubtitleExporterTests
{
    [Fact]
    public void ExportSrt_BuildsBilingualLines()
    {
        var segments = new List<SubtitleSegment>
        {
            new SubtitleSegment
            {
                Id = 1,
                Start = TimeSpan.FromSeconds(1),
                End = TimeSpan.FromSeconds(2),
                SourceText = "Hello",
                ZhText = "你好"
            }
        };

        var srt = SubtitleExporter.ExportSrt(segments);
        Assert.Contains("Hello", srt);
        Assert.Contains("你好", srt);
        Assert.Contains("00:00:01,000", srt);
    }

    [Fact]
    public void ExportAss_BuildsDialogueLines()
    {
        var segments = new List<SubtitleSegment>
        {
            new SubtitleSegment
            {
                Id = 1,
                Start = TimeSpan.FromSeconds(1),
                End = TimeSpan.FromSeconds(2),
                SourceText = "Hello",
                ZhText = "你好"
            }
        };

        var ass = SubtitleExporter.ExportAss(segments, "Default");
        Assert.Contains("Dialogue", ass);
        Assert.Contains("Hello", ass);
        Assert.Contains("你好", ass);
    }
}
