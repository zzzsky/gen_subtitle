using System.Globalization;
using System.Text;
using GenSubtitle.Core.Models;

namespace GenSubtitle.Core.Services;

public static class SubtitleExporter
{
    public static string ExportSrt(IEnumerable<SubtitleSegment> segments)
    {
        return SrtParser.Serialize(segments, s => string.IsNullOrWhiteSpace(s.ZhText)
            ? s.SourceText
            : $"{s.SourceText}\n{s.ZhText}");
    }

    public static string ExportAss(IEnumerable<SubtitleSegment> segments, string styleName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Script Info]");
        sb.AppendLine("ScriptType: v4.00+");
        sb.AppendLine("PlayResX: 1920");
        const int playResY = 1080;
        const int playResX = 1920;
        var fontSize = (int)Math.Round(playResY * 0.08, MidpointRounding.AwayFromZero);
        sb.AppendLine($"PlayResX: {playResX}");
        sb.AppendLine($"PlayResY: {playResY}");
        sb.AppendLine();
        sb.AppendLine("[V4+ Styles]");
        sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
        sb.AppendLine($"Style: {styleName},Microsoft YaHei,{fontSize},&H00FFFFFF,&H00000000,&H00000000,&H80000000,0,0,0,0,100,100,0,0,3,0,0,2,40,40,48,1");
        sb.AppendLine();
        sb.AppendLine("[Events]");
        sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

        foreach (var segment in segments)
        {
            var text = string.IsNullOrWhiteSpace(segment.ZhText)
                ? EscapeAss(segment.SourceText)
                : $"{EscapeAss(segment.SourceText)}\\N{EscapeAss(segment.ZhText)}";

            sb.AppendLine($"Dialogue: 0,{FormatAssTime(segment.Start)},{FormatAssTime(segment.End)},{styleName},,0,0,0,,{text}");
        }

        return sb.ToString();
    }

    private static string FormatAssTime(TimeSpan value)
    {
        return value.ToString("h\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture);
    }

    private static string EscapeAss(string value)
    {
        return value.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
    }
}
