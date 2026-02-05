using System.Globalization;
using System.Text;
using System.Linq;
using GenSubtitle.Core.Models;

namespace GenSubtitle.Core.Services;

public static class SrtParser
{
    public static List<SubtitleSegment> Parse(string content)
    {
        var segments = new List<SubtitleSegment>();
        using var reader = new StringReader(content);
        string? line;

        while (true)
        {
            line = ReadNonEmptyLine(reader);
            if (line is null)
            {
                break;
            }

            if (!int.TryParse(line.Trim(), out var index))
            {
                continue;
            }

            var timeLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(timeLine))
            {
                continue;
            }

            var times = timeLine.Split(" --> ");
            if (times.Length != 2)
            {
                continue;
            }

            var start = ParseTime(times[0].Trim());
            var end = ParseTime(times[1].Trim());
            var textBuilder = new StringBuilder();

            while ((line = reader.ReadLine()) != null && !string.IsNullOrWhiteSpace(line))
            {
                if (textBuilder.Length > 0)
                {
                    textBuilder.AppendLine();
                }
                textBuilder.Append(line);
            }

            segments.Add(new SubtitleSegment
            {
                Id = index,
                Start = start,
                End = end,
                SourceText = CleanSourceText(textBuilder.ToString()),
                ZhText = string.Empty
            });
        }

        return segments;
    }

    public static string Serialize(IEnumerable<SubtitleSegment> segments, Func<SubtitleSegment, string> selector)
    {
        var sb = new StringBuilder();
        var index = 1;
        foreach (var segment in segments)
        {
            sb.AppendLine(index.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine($"{FormatTime(segment.Start)} --> {FormatTime(segment.End)}");
            sb.AppendLine(selector(segment));
            sb.AppendLine();
            index++;
        }

        return sb.ToString();
    }

    private static string? ReadNonEmptyLine(StringReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                return line;
            }
        }
        return null;
    }

    private static TimeSpan ParseTime(string value)
    {
        var normalized = value.Replace(',', '.').Trim();
        if (TimeSpan.TryParseExact(normalized, "hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var ts))
        {
            return ts;
        }
        if (TimeSpan.TryParseExact(normalized, "h\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture, out ts))
        {
            return ts;
        }
        if (TimeSpan.TryParseExact(normalized, "hh\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture, out ts))
        {
            return ts;
        }
        if (TimeSpan.TryParseExact(normalized, "h\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture, out ts))
        {
            return ts;
        }

        throw new FormatException($"Invalid SRT time: '{value}'");
    }

    private static string FormatTime(TimeSpan value)
    {
        return value.ToString("hh\\:mm\\:ss\\,fff", CultureInfo.InvariantCulture);
    }

    private static string CleanSourceText(string text)
    {
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(line => line.TrimStart())
            .Select(line =>
            {
                if (line.StartsWith(">>"))
                {
                    return line.TrimStart('>', ' ');
                }
                return line;
            });
        return string.Join(Environment.NewLine, lines).Trim();
    }
}
