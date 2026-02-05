using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GenSubtitle.Core.Models;

namespace GenSubtitle.Core.Services;

public static class BilingualSubtitleIO
{
    public static void WriteBilingualSrt(string path, IList<SubtitleSegment> segments)
    {
        var content = SubtitleExporter.ExportSrt(segments);
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    public static void WriteBilingualAss(string path, IList<SubtitleSegment> segments, string styleName)
    {
        var content = SubtitleExporter.ExportAss(segments, styleName);
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    public static IList<SubtitleSegment> LoadFromSrt(string path)
    {
        var content = File.ReadAllText(path);
        return SrtParser.Parse(content);
    }

    public static void WriteMonoSrt(string path, IList<SubtitleSegment> segments, Func<SubtitleSegment, string> selector)
    {
        var sb = new StringBuilder();
        var index = 1;
        foreach (var segment in segments)
        {
            sb.AppendLine(index.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine($"{segment.Start:hh\\:mm\\:ss\\,fff} --> {segment.End:hh\\:mm\\:ss\\,fff}");
            sb.AppendLine(selector(segment));
            sb.AppendLine();
            index++;
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    public static void ApplyTranslationFromSrt(IList<SubtitleSegment> segments, string path)
    {
        var translated = LoadFromSrt(path);
        var count = Math.Min(segments.Count, translated.Count);
        for (var i = 0; i < count; i++)
        {
            segments[i].ZhText = translated[i].SourceText;
        }
    }
}
