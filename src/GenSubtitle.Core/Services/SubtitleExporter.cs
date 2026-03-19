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

    public static string ExportVtt(IEnumerable<SubtitleSegment> segments)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WEBVTT");
        sb.AppendLine();

        foreach (var segment in segments)
        {
            sb.AppendLine($"{FormatVttTime(segment.Start)} --> {FormatVttTime(segment.End)}");

            if (string.IsNullOrWhiteSpace(segment.ZhText))
            {
                sb.AppendLine(segment.SourceText);
            }
            else
            {
                sb.AppendLine($"{segment.SourceText}");
                sb.AppendLine($"{segment.ZhText}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string ExportTxt(IEnumerable<SubtitleSegment> segments)
    {
        var sb = new StringBuilder();

        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment.ZhText))
            {
                sb.AppendLine(segment.SourceText);
            }
            else
            {
                sb.AppendLine($"{segment.SourceText} {segment.ZhText}");
            }
        }

        return sb.ToString();
    }

    public static string ExportBilingualTable(IEnumerable<SubtitleSegment> segments)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#\tStart\tEnd\tSource Text\tChinese Text");

        foreach (var segment in segments)
        {
            sb.AppendLine($"{segment.Id}\t{segment.Start}\t{segment.End}\t{segment.SourceText}\t{segment.ZhText ?? ""}");
        }

        return sb.ToString();
    }

    public static string ExportSegments(IEnumerable<SubtitleSegment> segments, ExportFormat format, string styleName = "Default")
    {
        return format switch
        {
            ExportFormat.Srt => ExportSrt(segments),
            ExportFormat.Vtt => ExportVtt(segments),
            ExportFormat.Ass => ExportAss(segments, styleName),
            ExportFormat.Txt => ExportTxt(segments),
            ExportFormat.Bilingual => ExportBilingualTable(segments),
            _ => ExportSrt(segments)
        };
    }

    private static string FormatAssTime(TimeSpan value)
    {
        return value.ToString("h\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture);
    }

    private static string EscapeAss(string value)
    {
        return value.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
    }

    private static string FormatVttTime(TimeSpan value)
    {
        return value.ToString("hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture);
    }
}
