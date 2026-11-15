using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Software;

/// <summary>
/// Parses the fixed-width tables printed by <c>winget list</c> / <c>winget upgrade</c>:
/// <code>
/// Name            Id                 Version   Available Source
/// --------------------------------------------------------------
/// Git             Git.Git            2.44.0    2.47.1    winget
/// Notepad++       Notepad++.Notepad++ 8.6.2              winget
/// </code>
/// Columns are located from the header row's word positions. Positions are
/// measured in console display cells (East Asian wide characters and emoji
/// occupy two), because that is how winget pads its columns.
/// </summary>
public static partial class WingetTableParser
{
    private enum Column { Name, Id, Version, Available, Source, Other }

    private static readonly Dictionary<string, Column> KnownHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = Column.Name,
        ["Id"] = Column.Id,
        ["Version"] = Column.Version,
        ["Available"] = Column.Available,
        ["Source"] = Column.Source,
    };

    [GeneratedRegex(@"\x1B\[[0-9;?]*[ -/]*[@-~]")]
    private static partial Regex AnsiEscape();

    public static IReadOnlyList<InstalledApp> Parse(string output)
    {
        var apps = new List<InstalledApp>();
        if (string.IsNullOrEmpty(output)) return apps;

        var lines = SplitLines(output);

        for (var i = 1; i < lines.Count; i++)
        {
            if (!IsSeparator(lines[i])) continue;

            var columns = ParseHeader(lines[i - 1]);
            if (columns is null) continue;

            for (var j = i + 1; j < lines.Count; j++)
            {
                var row = lines[j];
                if (string.IsNullOrWhiteSpace(row) || IsSeparator(row)) break;
                // The next line is a separator: this row is another table's header.
                if (j + 1 < lines.Count && IsSeparator(lines[j + 1])) break;

                var app = ParseRow(row, columns);
                if (app is not null) apps.Add(app);
            }
        }

        return apps;
    }

    /// <summary>
    /// Splits output into lines. winget redraws its progress spinner with
    /// carriage returns, so only the text after the last '\r' on a line is kept.
    /// </summary>
    public static List<string> SplitLines(string output)
    {
        var result = new List<string>();
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var cr = line.LastIndexOf('\r');
            if (cr >= 0) line = line[(cr + 1)..];
            line = AnsiEscape().Replace(line, string.Empty);
            result.Add(line.TrimEnd());
        }
        return result;
    }

    private static bool IsSeparator(string line)
    {
        var t = line.Trim();
        return t.Length >= 3 && t.All(c => c == '-' || c == '─');
    }

    private static List<(Column Column, int Start)>? ParseHeader(string header)
    {
        var tokens = new List<(string Text, int Start)>();
        var cells = ToCells(header);
        var k = 0;
        while (k < cells.Count)
        {
            if (char.IsWhiteSpace(cells[k].Text[0])) { k++; continue; }
            var start = cells[k].Col;
            var sb = new StringBuilder();
            while (k < cells.Count && !char.IsWhiteSpace(cells[k].Text[0]))
                sb.Append(cells[k++].Text);
            tokens.Add((sb.ToString(), start));
        }

        if (tokens.Count < 3) return null;

        // English headers: map by name.
        if (tokens.All(t => KnownHeaders.ContainsKey(t.Text)))
        {
            var mapped = tokens.Select(t => (KnownHeaders[t.Text], t.Start)).ToList();
            return mapped.Any(m => m.Item1 == Column.Id) ? mapped : null;
        }

        // Other UI languages: fall back to winget's fixed column order.
        Column[]? order = tokens.Count switch
        {
            3 => [Column.Name, Column.Id, Column.Version],
            4 => [Column.Name, Column.Id, Column.Version, Column.Source],
            5 => [Column.Name, Column.Id, Column.Version, Column.Available, Column.Source],
            _ => null,
        };
        return order?.Select((c, idx) => (c, tokens[idx].Start)).ToList();
    }

    private static InstalledApp? ParseRow(string row, List<(Column Column, int Start)> columns)
    {
        var cells = ToCells(row);
        var values = new Dictionary<Column, string>();

        for (var c = 0; c < columns.Count; c++)
        {
            var start = columns[c].Start;
            var end = c + 1 < columns.Count ? columns[c + 1].Start : int.MaxValue;
            var sb = new StringBuilder();
            foreach (var cell in cells)
            {
                if (cell.Col >= start && cell.Col < end) sb.Append(cell.Text);
            }
            values[columns[c].Column] = sb.ToString().Trim();
        }

        var name = values.GetValueOrDefault(Column.Name, string.Empty);
        var id = values.GetValueOrDefault(Column.Id, string.Empty);

        // Footer lines ("3 upgrades available.") and misaligned rows have no usable ID.
        if (name.Length == 0 || id.Length == 0 || id.Contains(' ')) return null;

        return new InstalledApp
        {
            Name = name,
            Id = id,
            InstalledVersion = NullIfEmpty(values.GetValueOrDefault(Column.Version)),
            AvailableVersion = NullIfEmpty(values.GetValueOrDefault(Column.Available)),
            Source = NullIfEmpty(values.GetValueOrDefault(Column.Source)),
        };
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>Splits a line into text elements with their starting display column.</summary>
    private static List<(string Text, int Col)> ToCells(string line)
    {
        var cells = new List<(string, int)>();
        var col = 0;
        var e = StringInfo.GetTextElementEnumerator(line);
        while (e.MoveNext())
        {
            var text = e.GetTextElement();
            cells.Add((text, col));
            col += DisplayWidth(text);
        }
        return cells;
    }

    internal static int DisplayWidth(string textElement)
    {
        if (Rune.DecodeFromUtf16(textElement, out var rune, out _) != OperationStatus.Done)
            return 1; // malformed text: count as one cell
        var v = rune.Value;

        if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark)
            return 0;

        var wide =
            (v >= 0x1100 && v <= 0x115F) ||   // Hangul Jamo
            (v >= 0x2E80 && v <= 0x303E) ||   // CJK radicals, punctuation
            (v >= 0x3041 && v <= 0x33FF) ||   // Kana, CJK compatibility
            (v >= 0x3400 && v <= 0x4DBF) ||   // CJK Ext A
            (v >= 0x4E00 && v <= 0x9FFF) ||   // CJK Unified
            (v >= 0xA000 && v <= 0xA4CF) ||   // Yi
            (v >= 0xAC00 && v <= 0xD7A3) ||   // Hangul syllables
            (v >= 0xF900 && v <= 0xFAFF) ||   // CJK compatibility ideographs
            (v >= 0xFE30 && v <= 0xFE4F) ||   // CJK compatibility forms
            (v >= 0xFF00 && v <= 0xFF60) ||   // Fullwidth forms
            (v >= 0xFFE0 && v <= 0xFFE6) ||
            (v >= 0x1F300 && v <= 0x1FAFF) || // Emoji
            (v >= 0x20000 && v <= 0x3FFFD);   // CJK Ext B+

        return wide ? 2 : 1;
    }
}
