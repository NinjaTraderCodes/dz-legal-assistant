using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace DzLegal.Law.Corpus;

/// <summary>Strict, offline corpus reader. This does not replace the existing LawLibraryLoader.</summary>
public static partial class CorpusLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        MaxDepth = 64,
    };

    private static readonly string[] Checks = ["text-layer-matches-ocr", "discrepancy-resolved-from-image", "french-structure-matches", "inventory-complete", "amendment-applied"];
    private static readonly string[] States = ["checked", "partly-checked", "unsettled"];
    private static readonly string[] TextHosts = ["joradp.dz", "mjustice.gov.dz", "mjustice.dz", "mtess.gov.dz", "commerce.gov.dz"];

    public static LawCorpus Load(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        var codes = new List<CorpusCode>();
        foreach (var path in Directory.EnumerateDirectories(folder).Order(StringComparer.Ordinal))
        {
            if (Path.GetFileName(path) is "schema" or "research" || Path.GetFileName(path).StartsWith('.'))
            {
                continue;
            }

            codes.Add(new(Read<CorpusLedger>(Path.Combine(path, "ledger.json")),
                Read<CorpusArticlesFile>(Path.Combine(path, "articles.json")),
                Read<CorpusReport>(Path.Combine(path, "report.json"))));
        }

        return Validate(codes);
    }

    public static LawCorpus Validate(IReadOnlyList<CorpusCode> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        var errors = new List<string>();
        var codeIds = new HashSet<string>(StringComparer.Ordinal);
        var articleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in codes)
        {
            var ledger = code.Ledger;
            if (!CodeIdPattern().IsMatch(ledger.Id) || !codeIds.Add(ledger.Id))
            {
                errors.Add($"Invalid/duplicate code id: {ledger.Id}.");
            }

            if (ledger.SchemaVersion != "1.0" || code.Content.SchemaVersion != "1.0" || code.Report.SchemaVersion != "1.0")
            {
                errors.Add($"{ledger.Id}: unsupported schema version.");
            }

            if (ledger.Id != code.Content.CodeId || ledger.Id != code.Report.CodeId)
            {
                errors.Add($"{ledger.Id}: ledger/articles/report identities differ.");
            }

            if (string.IsNullOrWhiteSpace(ledger.CitationName) || string.IsNullOrWhiteSpace(ledger.Reference))
            {
                errors.Add($"{ledger.Id}: citation name/reference is empty.");
            }

            if (ledger.Scope.Kind is not ("priority-subset" or "complete-code")
                || (ledger.Scope.Complete && ledger.Scope.Kind != "complete-code"))
            {
                errors.Add($"{ledger.Id}: invalid coverage scope.");
            }

            if (ledger.Scope.AmendmentDiscoveryThrough == default || code.Report.GeneratedOn == default)
            {
                errors.Add($"{ledger.Id}: missing discovery/report date.");
            }

            var publications = new Dictionary<string, CorpusPublication>(StringComparer.Ordinal);
            foreach (var publication in ledger.Publications)
            {
                if (string.IsNullOrWhiteSpace(publication.Id) || !publications.TryAdd(publication.Id, publication))
                {
                    errors.Add($"{ledger.Id}: duplicate/empty publication id {publication.Id}.");
                }

                if (publication.Kind is not ("law" or "ordinance" or "decree" or "rectification")
                    || string.IsNullOrWhiteSpace(publication.Number) || publication.Date == default
                    || publication.Jo.Date < publication.Date || publication.Jo.Pages.Count == 0
                    || publication.Jo.Pages.Any(p => p < 1) || string.IsNullOrWhiteSpace(publication.Jo.Issue))
                {
                    errors.Add($"{publication.Id}: invalid publication kind, date or journal pages.");
                }

                ValidateDownload(publication.Arabic, publication.Id + ":Arabic", errors);
                if (publication.French is { } french)
                {
                    ValidateDownload(french, publication.Id + ":French", errors);
                }

                if (publication.CommencementDate is not null && string.IsNullOrWhiteSpace(publication.CommencementBasis))
                {
                    errors.Add($"{publication.Id}: commencement requires a recorded basis.");
                }

                if (publication.Discovery.Count == 0 || publication.Discovery.Any(d => !IsOfficial(d.Url, discovery: true) || d.AccessedOn == default || string.IsNullOrWhiteSpace(d.Note)))
                {
                    errors.Add($"{publication.Id}: missing/invalid official discovery provenance.");
                }
            }

            foreach (var article in code.Content.Articles)
            {
                if (!NumberPattern().IsMatch(article.CanonicalNumber)
                    || CanonicalNumber(article.Number) != article.CanonicalNumber
                    || article.Id != ArticleId(ledger.Id, article.CanonicalNumber) || !articleIds.Add(article.Id))
                {
                    errors.Add($"Invalid/duplicate article identity: {article.Id}.");
                }

                if (article.Availability is not ("text" or "reference-only")
                    || (article.Availability == "text" && article.Versions.Count == 0)
                    || (article.Availability == "reference-only" && article.Versions.Count != 0))
                {
                    errors.Add($"{article.Id}: text/reference-only availability disagrees with versions.");
                }

                var versionIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var version in article.Versions)
                {
                    ValidateVersion(article, version, publications, versionIds, errors);
                }

                var intervals = article.Versions.Where(v => v.InForceFrom is not null)
                    .OrderBy(v => v.InForceFrom).ToList();
                for (var i = 1; i < intervals.Count; i++)
                {
                    if (intervals[i - 1].InForceTo is null || intervals[i - 1].InForceTo > intervals[i].InForceFrom)
                    {
                        errors.Add($"{article.Id}: overlapping version intervals.");
                    }
                }
            }

            ValidateReport(code, publications, errors);
        }

        foreach (var reference in codes.SelectMany(c => c.Content.Articles).SelectMany(a => a.Versions)
            .SelectMany(v => v.CrossReferences.Select(r => (Version: v, Reference: r))))
        {
            if (!articleIds.Contains(reference.Reference.TargetId))
            {
                errors.Add($"{reference.Version.VersionId}: cross-reference target is absent: {reference.Reference.TargetId}.");
            }
        }

        if (errors.Count > 0)
        {
            throw new CorpusValidationException(errors);
        }

        return new LawCorpus(codes);
    }

    public static string ArticleId(string codeId, string canonicalNumber) =>
        codeId + ":" + canonicalNumber.Replace(" مكرر", "-bis", StringComparison.Ordinal).Replace(' ', '-');

    /// <summary>Also accepts the explicit thousands separators printed as 1.002 in the 1975 issue.</summary>
    public static string CanonicalNumber(string printedNumber)
    {
        ArgumentNullException.ThrowIfNull(printedNumber);
        var value = Domain.ArabicScript.ToAsciiDigits(printedNumber).Trim();
        if (PrintedThousandsPattern().IsMatch(value))
        {
            value = value.Replace(".", string.Empty, StringComparison.Ordinal);
        }

        return ArticleNumbers.Canonical(value);
    }

    private static T Read<T>(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
                ?? throw new CorpusValidationException([$"{path}: null JSON document."]);
        }
        catch (Exception ex) when (ex is IOException or JsonException or NotSupportedException)
        {
            throw new CorpusValidationException([$"{path}: {ex.Message}"]);
        }
    }

    private static void ValidateDownload(CorpusDownload source, string where, List<string> errors)
    {
        if (!IsOfficial(source.Url, discovery: false) || !HashPattern().IsMatch(source.Sha256) || source.RetrievedOn == default)
        {
            errors.Add($"{where}: source needs official HTTPS URL, SHA-256 and retrieval date.");
        }
    }

    private static bool IsOfficial(string url, bool discovery) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
        && string.IsNullOrEmpty(uri.UserInfo)
        && TextHosts.Concat(discovery ? ["coursupreme.dz", "conseildetat.dz"] : Array.Empty<string>())
            .Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));

    private static void ValidateVersion(CorpusArticle article, CorpusArticleVersion version,
        Dictionary<string, CorpusPublication> publications, HashSet<string> versionIds, List<string> errors)
    {
        var id = version.VersionId;
        if (!id.StartsWith(article.Id + "@", StringComparison.Ordinal) || !versionIds.Add(id))
        {
            errors.Add($"{article.Id}: invalid/duplicate version identity {id}.");
        }

        if (version.Status is not ("in-force" or "repealed") || version.TemporalStatus is not ("resolved" or "unsettled")
            || (version.TemporalStatus == "resolved" && version.InForceFrom is null)
            || (version.InForceFrom is { } from && version.InForceTo is { } to && to <= from))
        {
            errors.Add($"{id}: invalid status or temporal interval (end is exclusive).");
        }

        if (!publications.TryGetValue(version.IntroducedBy, out var publication)
            || version.Source.PublicationId != version.IntroducedBy)
        {
            errors.Add($"{id}: introducedBy/source publication is missing or inconsistent.");
        }
        else if (!publication.ArticlesTouched.Contains(article.CanonicalNumber, StringComparer.Ordinal))
        {
            errors.Add($"{id}: publication does not record touching this article.");
        }

        if (version.Source.Page < 1 || (version.Source.EndPage is { } end && end < version.Source.Page))
        {
            errors.Add($"{id}: invalid source PDF page range.");
        }

        foreach (var fragment in version.Source.Fragments)
        {
            if (!publications.ContainsKey(fragment.PublicationId) || fragment.Page < 1
                || string.IsNullOrWhiteSpace(fragment.EvidenceId)
                || (fragment.Region.Count != 0 && (fragment.Region.Count != 4 || fragment.Region.Any(n => n < 0 || n > 1)))
                || (fragment.Paragraph is { } p && (p < 1 || p > version.Paragraphs.Count)))
            {
                errors.Add($"{id}: invalid source fragment publication, page, region or paragraph.");
            }
        }

        if (version.Status != "repealed" && (string.IsNullOrWhiteSpace(version.Text) || version.Paragraphs.Count == 0))
        {
            errors.Add($"{id}: operative text/paragraphs are empty.");
        }

        if (version.Text != string.Join("\n", version.Paragraphs))
        {
            errors.Add($"{id}: paragraphs must reconstruct text exactly with LF separators.");
        }

        foreach (var reference in version.CrossReferences)
        {
            if (reference.Paragraph < 1 || reference.Paragraph > version.Paragraphs.Count || string.IsNullOrWhiteSpace(reference.Quote)
                || !version.Paragraphs[reference.Paragraph - 1].Contains(reference.Quote, StringComparison.Ordinal))
            {
                errors.Add($"{id}: invalid cross-reference source paragraph/quote.");
            }
        }

        if (!States.Contains(version.Verification.State, StringComparer.Ordinal)
            || version.Verification.Checks.Any(c => !Checks.Contains(c.Check, StringComparer.Ordinal)
                || c.Result != "passed" || c.Date == default || string.IsNullOrWhiteSpace(c.Method)))
        {
            errors.Add($"{id}: invalid verification state/check.");
        }

        var passed = version.Verification.Checks.Select(c => c.Check).ToHashSet(StringComparer.Ordinal);
        var textHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(version.Text)));
        foreach (var check in version.Verification.Checks.Where(c => c.Check is "text-layer-matches-ocr" or "discrepancy-resolved-from-image"))
        {
            if (!string.Equals(check.TextSha256, textHash, StringComparison.OrdinalIgnoreCase)
                || (check.Check == "discrepancy-resolved-from-image"
                    && (check.ImageSha256s is not { Count: > 0 } images || images.Any(h => !HashPattern().IsMatch(h)))))
            {
                errors.Add($"{id}: transcription check is not bound to the current text/image hashes.");
            }
        }

        if (version.Verification.State == "checked"
            && (!(passed.Contains("text-layer-matches-ocr") || passed.Contains("discrepancy-resolved-from-image"))
                || !passed.Contains("inventory-complete") || !passed.Contains("amendment-applied")))
        {
            errors.Add($"{id}: checked lacks the required transcription, inventory or amendment checks.");
        }

        if (version.Verification.State != "checked" && version.Verification.Reasons.Count == 0)
        {
            errors.Add($"{id}: incomplete verification needs explicit reasons.");
        }
    }

    private static void ValidateReport(CorpusCode code, Dictionary<string, CorpusPublication> publications, List<string> errors)
    {
        var report = code.Report;
        var inventory = report.Inventory;
        var present = code.Content.Articles.Where(a => a.Availability == "text").Select(a => a.CanonicalNumber).ToHashSet(StringComparer.Ordinal);
        var expected = inventory.Expected.ToHashSet(StringComparer.Ordinal);
        if (!present.SetEquals(inventory.Present) || !expected.Except(present).ToHashSet(StringComparer.Ordinal).SetEquals(inventory.Gaps)
            || (inventory.Complete && (inventory.Gaps.Count > 0 || inventory.UnappliedPublications.Count > 0))
            || inventory.UnappliedPublications.Any(p => !publications.ContainsKey(p)))
        {
            errors.Add($"{code.Ledger.Id}: inventory/report disagrees with corpus.");
        }

        if (code.Ledger.Scope.Complete && (!inventory.Complete || code.Ledger.Scope.Unsettled.Count > 0))
        {
            errors.Add($"{code.Ledger.Id}: complete scope has gaps/unsettled discovery.");
        }

        var versions = code.Content.Articles.SelectMany(a => a.Versions).ToList();
        foreach (var state in States)
        {
            if (report.CountsByState.GetValueOrDefault(state) != versions.Count(v => v.Verification.State == state))
            {
                errors.Add($"{code.Ledger.Id}: incorrect verification count for {state}.");
            }
        }

        foreach (var version in versions.Where(v => v.Verification.State != "checked" || v.TemporalStatus == "unsettled"))
        {
            if (!report.Unsettled.Any(u => u.Id == version.VersionId && !string.IsNullOrWhiteSpace(u.Reason)))
            {
                errors.Add($"{version.VersionId}: unresolved state missing from report.");
            }
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex CodeIdPattern();
    [GeneratedRegex("^[1-9][0-9]*(?:-[1-9][0-9]*)?(?: مكرر(?: [1-9][0-9]*)?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();
    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashPattern();
    [GeneratedRegex("^[1-9][0-9]{0,2}(?:\\.[0-9]{3})+$", RegexOptions.CultureInvariant)]
    private static partial Regex PrintedThousandsPattern();
}

public sealed class CorpusValidationException(IReadOnlyList<string> errors) : Exception(string.Join(Environment.NewLine, errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
