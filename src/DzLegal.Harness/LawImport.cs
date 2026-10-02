using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DzLegal.Domain.Serialization;
using DzLegal.Law;

namespace DzLegal.Harness;

/// <summary>
/// Maintainer tool: imports an official text into <c>data/law/codes/&lt;id&gt;.json</c>. This is the only part of
/// the project that downloads anything, it runs at development time, and the app itself never connects.
/// </summary>
public static partial class LawImport
{
    private static readonly System.Globalization.CultureInfo Ci = System.Globalization.CultureInfo.InvariantCulture;

    public static async Task<int> RunAsync(string id, string? url, string? file, string lawRoot, string? asOf, TextWriter output)
    {
        var entry = LawSourceManifest.Load(lawRoot).Codes.FirstOrDefault(c => c.Id == id);
        if (entry is null)
        {
            output.WriteLine($"law import: '{id}' is not in {Path.Combine(lawRoot, LawSourceManifest.FileName)}.");
            return 2;
        }

        if (url is null == (file is null))
        {
            output.WriteLine("law import: give exactly one of --url <official url> or --file <downloaded file>.");
            return 2;
        }

        byte[] bytes;
        if (file is not null)
        {
            bytes = await File.ReadAllBytesAsync(file);
        }
        else
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            bytes = await http.GetByteArrayAsync(new Uri(url!));
        }

        var text = ExtractText(bytes, file ?? url!);
        var parsed = LawTextParser.Parse(text);
        var code = new LawCode
        {
            Id = entry.Id,
            Title = entry.Title,
            CitationName = entry.CitationName,
            Reference = entry.Reference,
            ConsolidatedAsOf = asOf,
            Source = new LawSource
            {
                Url = url ?? Path.GetFileName(file!),
                Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
                RetrievedOn = DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            },
            Verified = false,
            Articles = parsed.Articles,
        };

        var errors = LawLibraryLoader.Validate(code);
        var codesFolder = Path.Combine(lawRoot, LawLibraryLoader.CodesFolder);
        Directory.CreateDirectory(codesFolder);
        var target = Path.Combine(codesFolder, id + ".json");
        var report = new StringBuilder();
        report.AppendLine(Ci, $"# Import report: {entry.Title.En} ({id})").AppendLine();
        report.AppendLine(Ci, $"- Source: {code.Source.Url}").AppendLine(Ci, $"- SHA-256: `{code.Source.Sha256}`").AppendLine(Ci, $"- Retrieved: {code.Source.RetrievedOn}");
        report.AppendLine(Ci, $"- Articles: {parsed.Articles.Count} ({parsed.Articles.Count(a => a.Repealed)} repealed)").AppendLine();
        if (File.Exists(target))
        {
            var diff = LawDiff.Compare(LawLibraryLoader.LoadCode(target), code);
            report.AppendLine("## Changes since the previous import").AppendLine();
            report.AppendLine(diff.IsEmpty ? "None." : $"- Added: {List(diff.Added)}\n- Removed: {List(diff.Removed)}\n- Text changed: {List(diff.Changed)}\n- Newly repealed: {List(diff.NewlyRepealed)}").AppendLine();
        }

        report.AppendLine("## For review").AppendLine();
        foreach (var w in parsed.Warnings.Concat(errors))
        {
            report.AppendLine(Ci, $"- {w}");
        }

        if (parsed.Warnings.Count + errors.Count == 0)
        {
            report.AppendLine("Nothing unusual found. Still compare a sample of articles with the official text.");
        }

        output.Write(report);
        if (errors.Count > 0 || parsed.Articles.Count == 0)
        {
            output.WriteLine("Not written: fix the problems above first.");
            return 1;
        }

        await File.WriteAllTextAsync(target, JsonSerializer.Serialize(code, DefinitionJson.Options) + "\n");
        var reports = Path.Combine(lawRoot, "reports");
        Directory.CreateDirectory(reports);
        await File.WriteAllTextAsync(Path.Combine(reports, id + ".md"), report.ToString());
        output.WriteLine($"Written: {target}");
        return 0;
    }

    private static string List(IReadOnlyList<string> numbers) => numbers.Count == 0 ? "none" : string.Join(", ", numbers.Take(50)) + (numbers.Count > 50 ? ", …" : string.Empty);

    /// <summary>PDF (through poppler's pdftotext), HTML (tags removed) or plain UTF-8 text.</summary>
    public static string ExtractText(byte[] bytes, string name)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length > 4 && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F')
        {
            return PdfToText(bytes);
        }

        var text = Encoding.UTF8.GetString(bytes);
        var isHtml = name.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            || text.Contains("<html", StringComparison.OrdinalIgnoreCase);
        return isHtml ? HtmlToText(text) : text;
    }

    public static string HtmlToText(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        var noScripts = Scripts().Replace(html, " ");
        var withBreaks = Breaks().Replace(noScripts, "\n");
        return WebUtility.HtmlDecode(Tags().Replace(withBreaks, string.Empty));
    }

    private static string PdfToText(byte[] bytes)
    {
        var temp = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(temp, bytes);
            var start = new ProcessStartInfo("pdftotext", ["-enc", "UTF-8", "-raw", temp, "-"])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            using var process = Process.Start(start) ?? throw new InvalidOperationException("pdftotext could not be started (install poppler-utils).");
            var text = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? text : throw new InvalidOperationException("pdftotext failed: " + process.StandardError.ReadToEnd());
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [GeneratedRegex(@"<(script|style)[\s\S]*?</\1>", RegexOptions.IgnoreCase)]
    private static partial Regex Scripts();

    [GeneratedRegex(@"<(br|/p|/div|/li|/tr|/h\d)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex Breaks();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tags();
}
