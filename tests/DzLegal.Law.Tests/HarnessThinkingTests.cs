using System.Globalization;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DzLegal.Drafting;
using DzLegal.Harness;
using DzLegal.ModelProviders.Ollama;

namespace DzLegal.Law.Tests;

public sealed class HarnessThinkingTests
{
    [Theory]
    [InlineData("facts")]
    [InlineData("grounds")]
    [InlineData("draft")]
    public void CommandsShareThinkingValidationWithoutChangingOmission(string command)
    {
        Assert.Null(HarnessModels.ThinkingOption([command, "--models", "gemma4:e4b"]));
        Assert.False(HarnessModels.ThinkingOption([command, "--think", "false"]));
        Assert.True(HarnessModels.ThinkingOption([command, "--think", "true"]));
        Assert.Throws<ArgumentException>(() => HarnessModels.ThinkingOption([command, "--think"]));
        Assert.Throws<ArgumentException>(() => HarnessModels.ThinkingOption([command, "--think", "invalid"]));
    }

    [Theory]
    [InlineData("grounds", null, false)]
    [InlineData("grounds", false, false)]
    [InlineData("draft", null, false)]
    [InlineData("draft", false, false)]
    [InlineData("draft", null, true)]
    [InlineData("draft", false, true)]
    public async Task RealCommandsPassTheOverrideAndPreserveOmittedDefaults(string command, bool? think, bool extraCases)
    {
        var folder = Directory.CreateTempSubdirectory("dz-alpha03-thinking-cli-");
        using var server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var requests = new List<string>();
        var pump = Task.Run(async () =>
        {
            try
            {
                while (!timeout.IsCancellationRequested)
                {
                    var body = await CaptureAsync(server, timeout.Token, allowMetadata: true);
                    if (body.Length > 0) { requests.Add(body); }
                }
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
            catch (SocketException) when (timeout.IsCancellationRequested) { }
        });
        try
        {
            var cases = Directory.CreateDirectory(Path.Combine(folder.FullName, "inputs")).FullName;
            await File.WriteAllTextAsync(Path.Combine(cases, "fixture.json"), command == "grounds"
                ? """{"cases":[{"name":"fixture","facts":"Fictitious notes.","expect":"","partition":"development"}]}"""
                : """{"name":"fixture","inputs":{"notes.facts":"اتفق الطرفان على بيع منقول.","notes.grounds":"لم ينفذ المدعى عليه الاتفاق ويلتمس المدعي إلزامه بتنفيذه.","notes.requests":"إلزام المدعى عليه بتنفيذ الاتفاق."}}""");
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = folder.FullName };
            foreach (var argument in new[]
            {
                "exec", "--runtimeconfig", Path.Combine(AppContext.BaseDirectory, "DzLegal.Law.Tests.runtimeconfig.json"),
                "--depsfile", Path.Combine(AppContext.BaseDirectory, "DzLegal.Law.Tests.deps.json"), typeof(HarnessModels).Assembly.Location,
                command, "--models", "gemma4:e4b", "--cases", cases, "--out", Path.Combine(folder.FullName, "output"),
                "--endpoint", $"http://127.0.0.1:{((IPEndPoint)server.LocalEndpoint).Port}",
                "--law-root", Path.Combine(AppContext.BaseDirectory, "data", "law"),
                "--data", Path.Combine(AppContext.BaseDirectory, "data", "document-types"),
            }) { start.ArgumentList.Add(argument); }
            if (extraCases)
            {
                var extra = Directory.CreateDirectory(Path.Combine(folder.FullName, "extra")).FullName;
                await File.WriteAllTextAsync(Path.Combine(extra, "another.json"), """{"name":"another","extends":"../inputs/fixture.json","inputs":{}}""");
                start.ArgumentList.Add("--extra-cases"); start.ArgumentList.Add(extra);
            }
            if (think.HasValue) { start.ArgumentList.Add("--think"); start.ArgumentList.Add(think.Value ? "true" : "false"); }
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            try { await process.WaitForExitAsync(timeout.Token); }
            finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); } }
            Assert.True(process.ExitCode == 0, await stdout + await stderr);
            if (extraCases)
            {
                Assert.Contains("2 cases", await File.ReadAllTextAsync(Path.Combine(folder.FullName, "output", "summary.md")), StringComparison.Ordinal);
                using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder.FullName, "output", "manifest.json")));
                Assert.True(manifest.RootElement.GetProperty("Fingerprints").TryGetProperty("extraCases", out _));
            }
        }
        finally
        {
            timeout.Cancel();
            server.Stop();
            await pump;
            Assert.StartsWith(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "dz-alpha03-thinking-cli-")), Path.GetFullPath(folder.FullName), StringComparison.OrdinalIgnoreCase);
            folder.Delete(recursive: true);
        }

        Assert.NotEmpty(requests);
        foreach (var request in requests)
        {
            using var body = JsonDocument.Parse(request);
            if (think.HasValue) { Assert.Equal(think.Value, body.RootElement.GetProperty("think").GetBoolean()); }
            else { Assert.False(body.RootElement.TryGetProperty("think", out _)); }
            Assert.Equal(0, body.RootElement.GetProperty("options").GetProperty("num_gpu").GetInt32());
            Assert.Equal(4096, body.RootElement.GetProperty("options").GetProperty("num_ctx").GetInt32());
        }
    }

    [Theory]
    [InlineData("gemma3:4b", null, null)]
    [InlineData("gemma4:e4b", null, null)]
    [InlineData("gemma4:e4b", false, false)]
    [InlineData("gemma4:e2b", false, false)]
    [InlineData("gemma3n:e2b", null, null)]
    [InlineData("gemma4:e4b", true, true)]
    [InlineData("qwen3:4b", null, false)]
    public async Task ExplicitThinkingReachesTheWireAndOmissionPreservesDefaults(string model, bool? think, bool? expected)
    {
        using var server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        var endpoint = new Uri($"http://127.0.0.1:{((IPEndPoint)server.LocalEndpoint).Port}/");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var capture = CaptureAsync(server, timeout.Token);
        using var provider = (OllamaModelProvider)HarnessModels.Create(model, endpoint, TimeSpan.FromSeconds(5), think: think);
        var answer = new StringBuilder();
        await foreach (var piece in provider.StreamAsync(new ModelRequest("facts.claim", "system", "question", "notes") { MaxTokens = ClaimFactReader.MaxTokens }, timeout.Token))
        {
            answer.Append(piece);
        }

        Assert.Equal("fixture", answer.ToString());
        using var body = JsonDocument.Parse(await capture);
        var root = body.RootElement;
        Assert.Equal(model, root.GetProperty("model").GetString());
        Assert.True(root.GetProperty("stream").GetBoolean());
        if (expected.HasValue)
        {
            Assert.Equal(expected.Value, root.GetProperty("think").GetBoolean());
        }
        else
        {
            Assert.False(root.TryGetProperty("think", out _));
        }

        Assert.Equal(300, root.GetProperty("options").GetProperty("num_predict").GetInt32());
        Assert.Equal(4096, root.GetProperty("options").GetProperty("num_ctx").GetInt32());
        Assert.Equal(0, root.GetProperty("options").GetProperty("num_gpu").GetInt32());
        Assert.Equal(.2, root.GetProperty("options").GetProperty("temperature").GetDouble());
        Assert.Equal(1.1, root.GetProperty("options").GetProperty("repeat_penalty").GetDouble());
    }

    [Fact]
    public void LocalThinkingOverrideCannotSilentlyApplyToHostedOrTemplateEngines()
    {
        Assert.Throws<ArgumentException>(() => HarnessModels.Create("stub", new Uri("http://127.0.0.1:9"), TimeSpan.FromSeconds(1), think: false));
        Assert.Throws<ArgumentException>(() => HarnessModels.Create("google", new Uri("http://127.0.0.1:9"), TimeSpan.FromSeconds(1), think: false));
    }

    private static async Task<string> CaptureAsync(TcpListener server, CancellationToken cancellationToken, bool allowMetadata = false)
    {
        using var client = await server.AcceptTcpClientAsync(cancellationToken);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync(cancellationToken);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadLineAsync(cancellationToken) is { Length: > 0 } line)
        {
            var parts = line.Split(':', 2);
            headers[parts[0]] = parts[1].Trim();
        }

        if (allowMetadata && requestLine is "GET /api/version HTTP/1.1" or "GET /api/tags HTTP/1.1")
        {
            var metadata = requestLine.Contains("/version", StringComparison.Ordinal) ? """{"version":"fixture"}""" : """{"models":[]}""";
            await ReplyAsync(stream, metadata, cancellationToken);
            return string.Empty;
        }

        Assert.Equal("POST /api/chat HTTP/1.1", requestLine);
        var body = new StringBuilder();
        if (headers.TryGetValue("Content-Length", out var length))
        {
            body.Append(await ReadAsync(reader, int.Parse(length, CultureInfo.InvariantCulture), cancellationToken));
        }
        else
        {
            Assert.Equal("chunked", headers["Transfer-Encoding"]);
            while (int.Parse((await reader.ReadLineAsync(cancellationToken))!.Split(';')[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture) is var size && size != 0)
            {
                body.Append(await ReadAsync(reader, size, cancellationToken));
                Assert.Equal(string.Empty, await reader.ReadLineAsync(cancellationToken));
            }
        }

        const string payload = """{"message":{"content":"fixture"},"done":true} """;
        await ReplyAsync(stream, payload, cancellationToken);
        return body.ToString();
    }

    private static async Task ReplyAsync(NetworkStream stream, string payload, CancellationToken cancellationToken)
    {
        var reply = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n\r\n{payload}");
        await stream.WriteAsync(reply, cancellationToken);
    }

    private static async Task<string> ReadAsync(StreamReader reader, int length, CancellationToken cancellationToken)
    {
        var chars = new char[length];
        var offset = 0;
        while (offset < length)
        {
            var read = await reader.ReadAsync(chars.AsMemory(offset), cancellationToken);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            offset += read;
        }

        return new string(chars);
    }
}
