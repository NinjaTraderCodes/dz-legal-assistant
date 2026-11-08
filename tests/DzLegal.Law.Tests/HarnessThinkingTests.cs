using System.Globalization;
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
    [InlineData("gemma3:4b", null, null)]
    [InlineData("gemma4:e4b", null, null)]
    [InlineData("gemma4:e4b", false, false)]
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

    private static async Task<string> CaptureAsync(TcpListener server, CancellationToken cancellationToken)
    {
        using var client = await server.AcceptTcpClientAsync(cancellationToken);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        Assert.Equal("POST /api/chat HTTP/1.1", await reader.ReadLineAsync(cancellationToken));
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadLineAsync(cancellationToken) is { Length: > 0 } line)
        {
            var parts = line.Split(':', 2);
            headers[parts[0]] = parts[1].Trim();
        }

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
        var reply = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n\r\n{payload}");
        await stream.WriteAsync(reply, cancellationToken);
        return body.ToString();
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
