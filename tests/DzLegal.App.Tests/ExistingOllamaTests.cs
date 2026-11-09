using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DzLegal.App;
using DzLegal.App.Services.LocalModels;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Tests;

public class ExistingOllamaTests
{
    private sealed class VersionHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }

    [Theory]
    [InlineData("{\"version\":\"0.30.8\"}", false)]
    [InlineData("{\"version\":\"0.30.9\"}", true)]
    [InlineData("{\"version\":\"0.40.15\"}", true)]
    [InlineData("{\"version\":\"0.40.16\"}", true)]
    [InlineData("{\"version\":\"1.0.0\"}", true)]
    [InlineData("{\"version\":\"0.30.9-rc1\"}", false)]
    [InlineData("{\"version\":\"unknown\"}", false)]
    [InlineData("{\"version\":\"0.40\"}", false)]
    [InlineData("{\"version\":123}", false)]
    [InlineData("{}", false)]
    public async Task ExistingVersionsHaveAMinimumRatherThanAnExactPin(string json, bool compatible)
    {
        using var api = new LocalOllamaApi(new Uri("http://127.0.0.1:11434"), new VersionHandler(json));
        Assert.Equal(compatible, await api.IsCompatibleAsync(CancellationToken.None));
    }

    [Fact]
    public void TheEngineKeepsOnlyTheHttpsProxyAndLoopbackBypasses()
    {
        var environment = new Dictionary<string, string>
        {
            ["HTTPS_PROXY"] = "http://proxy.example.invalid:8080",
            ["NO_PROXY"] = ".office.example.invalid",
            ["HTTP_PROXY"] = "http://unused.example.invalid:8080",
            ["ALL_PROXY"] = "http://unused.example.invalid:8080",
            ["OLLAMA_HOST"] = "0.0.0.0:11434",
            ["OLLAMA_NO_CLOUD"] = "0",
            ["SSL_CERT_FILE"] = "untrusted.pem",
        };
        var root = Path.Combine(Path.GetTempPath(), "dz-proxy-test");
        var info = WindowsEngineLauncher.StartInfo(Path.Combine(root, "ollama.exe"), root, 12345,
            name => environment.GetValueOrDefault(name));
        Assert.Equal(environment["HTTPS_PROXY"], info.Environment["HTTPS_PROXY"]);
        Assert.Equal(".office.example.invalid,127.0.0.1,localhost,::1", info.Environment["NO_PROXY"]);
        Assert.Equal("127.0.0.1:12345", info.Environment["OLLAMA_HOST"]);
        Assert.Equal("1", info.Environment["OLLAMA_NO_CLOUD"]);
        Assert.DoesNotContain("HTTP_PROXY", info.Environment.Keys);
        Assert.DoesNotContain("ALL_PROXY", info.Environment.Keys);
        Assert.DoesNotContain("SSL_CERT_FILE", info.Environment.Keys);
        using var handler = DzLegal.ModelProviders.Ollama.LoopbackGuard.CreateHandler();
        Assert.False(handler.UseProxy);
        Assert.False(handler.AllowAutoRedirect);
    }

    private sealed class ReusedEngine(Uri endpoint) : ILocalEngine
    {
        public bool IsManaged => false;
        public bool Disposed { get; private set; }
        public Task<LocalEngineSession> EnsureAsync(CancellationToken cancellationToken) => Task.FromResult(new LocalEngineSession(endpoint, false));
        public void Dispose() => Disposed = true;
    }

    [Fact]
    public async Task ReleasingAReusedServerUnloadsOnlyTheSelectedModel()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var loaded = new HashSet<string>(["gemma3:4b", "gemma4:e4b"]);
        var paths = new List<string>();
        var unloads = new List<string>();
        var serving = ServeAsync();
        using var engine = new ReusedEngine(endpoint);
        var settings = new AppSettings();
        var option = Assert.Single(Composition.CreateProviders(settings, engine), p => p.Key == "ollama");
        Assert.Equal(ModelState.Ready, (await option.ProbeAsync(timeout.Token)).State);
        await option.ReleaseAsync(timeout.Token);
        await serving;
        Assert.Equal(["/api/tags", "/api/show", "/api/generate"], paths);
        Assert.Equal(["gemma4:e4b"], unloads);
        Assert.Equal(["gemma3:4b"], loaded);
        Assert.False(engine.Disposed);

        async Task ServeAsync()
        {
            for (var i = 0; i < 3; i++)
            {
                using var client = await listener.AcceptTcpClientAsync(timeout.Token);
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                var request = await reader.ReadLineAsync(timeout.Token);
                var path = request!.Split(' ')[1];
                paths.Add(path);
                var length = 0;
                var chunked = false;
                string? header;
                while (!string.IsNullOrEmpty(header = await reader.ReadLineAsync(timeout.Token)))
                {
                    if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        length = int.Parse(header[15..], CultureInfo.InvariantCulture);
                    if (header.Equals("Transfer-Encoding: chunked", StringComparison.OrdinalIgnoreCase)) chunked = true;
                }
                var body = new StringBuilder();
                if (chunked)
                {
                    while (true)
                    {
                        var size = int.Parse((await reader.ReadLineAsync(timeout.Token))!, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        if (size == 0) { await reader.ReadLineAsync(timeout.Token); break; }
                        await ReadBodyAsync(size);
                        Assert.Equal("", await reader.ReadLineAsync(timeout.Token));
                    }
                }
                else if (length > 0) await ReadBodyAsync(length);

                async Task ReadBodyAsync(int count)
                {
                    var buffer = new char[count];
                    Assert.Equal(count, await reader.ReadBlockAsync(buffer.AsMemory(), timeout.Token));
                    body.Append(buffer);
                }
                var json = "{}";
                if (path == "/api/tags") json = "{\"models\":[{\"name\":\"gemma3:4b\"},{\"name\":\"gemma4:e4b\"}]}";
                else if (path == "/api/show") json = "{\"details\":{\"format\":\"gguf\"},\"model_info\":{\"general.architecture\":\"gemma4\"}}";
                else
                {
                    using var document = JsonDocument.Parse(body.ToString());
                    var selected = document.RootElement.GetProperty("model").GetString()!;
                    Assert.Equal(0, document.RootElement.GetProperty("keep_alive").GetInt32());
                    Assert.Equal(2, document.RootElement.EnumerateObject().Count());
                    unloads.Add(selected);
                    loaded.Remove(selected);
                }
                var bytes = Encoding.UTF8.GetBytes(json);
                var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(headers, timeout.Token);
                await stream.WriteAsync(bytes, timeout.Token);
            }
        }
    }
}