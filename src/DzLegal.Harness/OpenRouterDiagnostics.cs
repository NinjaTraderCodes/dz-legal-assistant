using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DzLegal.Harness;

/// <summary>
/// `ping` for an OpenAI-compatible service (OpenRouter, DeepInfra): tells whose limit stops it. Reads the key's own status (credit, free tier, rate limit), then sends
/// one tiny chat request and one with a drafting-sized answer cap, and prints the status, the rate-limit headers, the
/// service's explanation and which provider served the model. The key itself is never printed.
/// </summary>
public static class OpenRouterDiagnostics
{
    /// <param name="service">"OpenRouter" (reads the key's status) or "DeepInfra" (lists the Gemma models served).</param>
    public static async Task<int> RunAsync(Uri endpoint, string key, string model, TextWriter output, string service = "OpenRouter", HttpMessageHandler? handler = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(output);
        using var http = new HttpClient(handler ?? new HttpClientHandler(), disposeHandler: true) { BaseAddress = endpoint, Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        var openRouter = service == "OpenRouter";
        output.WriteLine($"{service} check for {model} at {endpoint} (key from {(openRouter ? "OPENROUTER_API_KEY" : "DEEPINFRA_API_KEY")}, not shown)");
        output.WriteLine();

        output.WriteLine(openRouter ? "1. Your key" : "1. Gemma models served");
        var keyOk = openRouter ? await KeyStatusAsync(http, output, cancellationToken) : await GemmaModelsAsync(http, model, output, cancellationToken);
        output.WriteLine();

        output.WriteLine("2. A tiny request (answer cap 5 tokens)");
        var tiny = await ChatAsync(http, model, 5, output, cancellationToken);
        output.WriteLine();

        output.WriteLine("3. A drafting-sized request (answer cap 900 tokens)");
        var large = await ChatAsync(http, model, 900, output, cancellationToken);
        output.WriteLine();

        output.WriteLine("Verdict: " + (openRouter ? Verdict(keyOk, tiny, large) : DirectVerdict(service, model, tiny, large)));
        return 0;
    }

    /// <summary>Lists the Gemma models an OpenAI-compatible service serves (GET models), marking the one under test.</summary>
    private static async Task<bool> GemmaModelsAsync(HttpClient http, string model, TextWriter output, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(new Uri("models", UriKind.Relative), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            output.WriteLine($"   HTTP {(int)response.StatusCode}");
            if (!response.IsSuccessStatusCode)
            {
                output.WriteLine("   " + Short(body));
                return false;
            }

            using var json = JsonDocument.Parse(body);
            var ids = (json.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array ? data.EnumerateArray() : default)
                .Select(m => m.TryGetProperty("id", out var id) ? id.GetString() : null)
                .OfType<string>()
                .Where(id => id.Contains("gemma", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .ToList();
            foreach (var id in ids)
            {
                output.WriteLine($"   {id}{(string.Equals(id, model, StringComparison.OrdinalIgnoreCase) ? "  ← under test" : string.Empty)}");
            }

            if (ids.Count == 0)
            {
                output.WriteLine("   none listed");
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            output.WriteLine("   not reachable: " + ex.Message);
            return false;
        }
    }

    /// <summary>The verdict for a service called directly with the user's own account (no shared pool in between).</summary>
    private static string DirectVerdict(string service, string model, ChatResult tiny, ChatResult large)
    {
        if (tiny.Status is 401 or 403)
        {
            return $"the key is refused. Check DEEPINFRA_API_KEY (a key from {service}'s dashboard).";
        }

        if (tiny.Status == 402 || large.Status == 402 || tiny.Explanation.Contains("balance", StringComparison.OrdinalIgnoreCase) || tiny.Explanation.Contains("payment", StringComparison.OrdinalIgnoreCase))
        {
            return $"{service} asks for credit or a payment method on this account before it answers.";
        }

        if (tiny.Status == 404 || tiny.Explanation.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return $"{model} is not served under this name. Use one of the Gemma IDs listed above: --models deepinfra:<id>.";
        }

        if (tiny.Status == 429 || large.Status == 429)
        {
            return $"{service} rate-limits your own account right now (not a shared pool). Wait a minute, or use --delay-seconds 5.";
        }

        if (tiny.Status == 200 && large.Status == 200)
        {
            return $"available. Run: --models deepinfra:{model}";
        }

        return $"unexpected: tiny HTTP {tiny.Status}, large HTTP {large.Status}. See the details above.";
    }

    private sealed record ChatResult(int Status, string Explanation, string? Provider);

    private static async Task<bool> KeyStatusAsync(HttpClient http, TextWriter output, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(new Uri("key", UriKind.Relative), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            output.WriteLine($"   HTTP {(int)response.StatusCode}");
            if (!response.IsSuccessStatusCode)
            {
                output.WriteLine("   " + Short(body));
                return false;
            }

            using var json = JsonDocument.Parse(body);
            var data = json.RootElement.TryGetProperty("data", out var d) ? d : json.RootElement;
            foreach (var name in new[] { "usage", "limit", "limit_remaining", "is_free_tier", "rate_limit" })
            {
                if (data.TryGetProperty(name, out var value))
                {
                    output.WriteLine($"   {name}: {value.GetRawText()}");
                }
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            output.WriteLine("   not reachable: " + ex.Message);
            return false;
        }
    }

    private static async Task<ChatResult> ChatAsync(HttpClient http, string model, int maxTokens, TextWriter output, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = new[] { new Dictionary<string, string> { ["role"] = "user", ["content"] = "أجب بكلمة واحدة: نعم" } },
            ["max_tokens"] = maxTokens,
            ["temperature"] = 0.2,
        });
        var clock = Stopwatch.StartNew();
        try
        {
            using var content = new StringContent(request, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(new Uri("chat/completions", UriKind.Relative), content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            output.WriteLine($"   HTTP {(int)response.StatusCode} in {clock.Elapsed.TotalSeconds:0.0} s");
            foreach (var header in response.Headers.Where(h => h.Key.StartsWith("x-ratelimit", StringComparison.OrdinalIgnoreCase) || h.Key.Equals("retry-after", StringComparison.OrdinalIgnoreCase)))
            {
                output.WriteLine($"   {header.Key}: {string.Join(",", header.Value)}");
            }

            string explanation = string.Empty;
            string? provider = null;
            try
            {
                using var json = JsonDocument.Parse(body);
                var root = json.RootElement;
                if (root.TryGetProperty("provider", out var p))
                {
                    provider = p.GetString();
                }

                if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
                {
                    var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
                    string? raw = null;
                    if (error.TryGetProperty("metadata", out var meta) && meta.ValueKind == JsonValueKind.Object)
                    {
                        raw = meta.TryGetProperty("raw", out var r) ? r.ToString() : null;
                        provider ??= meta.TryGetProperty("provider_name", out var pn) ? pn.GetString() : null;
                    }

                    explanation = string.Join(" — ", new[] { message, raw }.Where(x => !string.IsNullOrWhiteSpace(x)));
                }
                else if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    explanation = "answered: " + Short(choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty);
                }
            }
            catch (JsonException)
            {
                explanation = Short(body);
            }

            output.WriteLine($"   provider: {provider ?? "—"}");
            output.WriteLine($"   {explanation}");
            return new ChatResult((int)response.StatusCode, explanation, provider);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            output.WriteLine("   not reachable: " + ex.Message);
            return new ChatResult(0, ex.Message, null);
        }
    }

    private static string Verdict(bool keyOk, ChatResult tiny, ChatResult large)
    {
        if (!keyOk && tiny.Status is 401 or 403)
        {
            return "the key is refused. Check OPENROUTER_API_KEY on this PC (it may differ from the one on Render).";
        }

        if (tiny.Status == 402 || large.Status == 402)
        {
            return "no credit left on this key, or its credit limit is reached. Add credit or raise the key's limit.";
        }

        var upstream = new[] { tiny, large }.Any(r => r.Status == 429 && r.Explanation.Contains("upstream", StringComparison.OrdinalIgnoreCase));
        if (upstream)
        {
            return $"shared upstream limit ({tiny.Provider ?? large.Provider ?? "the model's provider"}), not your key. Every OpenRouter user of this model is limited at the moment; the website hides it by answering with Gemma 4 on Google. Retry later, or run with --delay-seconds 3.";
        }

        if (tiny.Status == 429 || large.Status == 429)
        {
            return "your key or account is rate-limited (not the upstream provider). Compare with the key used on Render, and the key's rate_limit above.";
        }

        if (tiny.Status == 200 && large.Status == 200)
        {
            return $"OpenRouter answers normally right now (served by {tiny.Provider ?? "?"}). A stalled run was a busy spell; rerun.";
        }

        return $"unexpected: tiny HTTP {tiny.Status}, large HTTP {large.Status}. See the details above.";
    }

    private static string Short(string text) => text.Length > 200 ? text[..200] + "…" : text.Replace('\n', ' ');
}
