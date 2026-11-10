using System.Net;
using System.Text;
using System.Text.Json;
using DzLegal.App.Services.LocalModels;

namespace DzLegal.App.Tests;

public class ModelRevisionTests
{
    private sealed class Metadata(SupportedModel model, string? digest = null, string? source = null, bool remote = false) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            var path = Path.Combine(Path.GetTempPath(), "fictitious-models", "sha256-" + (source ?? model.Blobs[0].Sha256));
            var json = request.RequestUri.AbsolutePath == "/api/tags"
                ? JsonSerializer.Serialize(new { models = new[] { new { name = model.Name, digest = digest ?? model.ManifestSha256 } } })
                : JsonSerializer.Serialize(new { modelfile = "FROM \"" + path + "\"\n", remote_host = remote ? "https://example.invalid" : "" });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    [Theory]
    [InlineData("gemma3:4b", "68ceab7e468a31c988b44d5a4984a0e21f58c4f7158c6ce29c1804babac38df7")]
    [InlineData("gemma3:4b", "c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291")]
    [InlineData("gemma4:e4b", "dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c")]
    public async Task DiscoveryReturnsTheExactInstalledPinnedRevisionWithoutPull(string name, string digest)
    {
        var known = SupportedModels.Versions(name).Single(m => m.ManifestSha256 == digest);
        using var handler = new Metadata(known);
        using var api = new LocalOllamaApi(new Uri("http://127.0.0.1:12345"), handler);
        var found = await api.LocationAsync(SupportedModels.Versions(name), CancellationToken.None);
        Assert.NotNull(found);
        Assert.Same(known, found.Value.Model);
        Assert.Equal(["/api/tags", "/api/show"], handler.Paths);
    }

    [Fact]
    public async Task ATagDigestOutsideTheBuiltInListIsNeverAccepted()
    {
        var model = SupportedModels.Find("gemma3:4b");
        using var handler = new Metadata(model, new string('f', 64));
        using var api = new LocalOllamaApi(new Uri("http://127.0.0.1:12345"), handler);
        Assert.Null(await api.LocationAsync(SupportedModels.Versions(model.Name), CancellationToken.None));
        Assert.Equal(["/api/tags"], handler.Paths);
    }

    [Fact]
    public async Task KnownRevisionsCannotMixTheirWeightPaths()
    {
        var current = SupportedModels.Find("gemma3:4b");
        var old = SupportedModels.Versions(current.Name).Single(m => m != current);
        using var handler = new Metadata(current, source: old.Blobs.Single(b => b.Size > 2_000_000_000).Sha256);
        using var api = new LocalOllamaApi(new Uri("http://127.0.0.1:12345"), handler);
        Assert.Null(await api.LocationAsync(SupportedModels.Versions(current.Name), CancellationToken.None));
    }

    [Fact]
    public async Task ARemoteAliasCannotBorrowAKnownRevisionDigest()
    {
        var model = SupportedModels.Find("gemma3:4b");
        using var handler = new Metadata(model, remote: true);
        using var api = new LocalOllamaApi(new Uri("http://127.0.0.1:12345"), handler);
        Assert.Null(await api.LocationAsync(SupportedModels.Versions(model.Name), CancellationToken.None));
    }

    [Fact]
    public void ThereAreStillTwoChoicesAndAllKnownRevisionsShareTheirDisplayedTerms()
    {
        Assert.Equal(["gemma3:4b", "gemma4:e4b"], SupportedModels.All.Select(m => m.Name));
        Assert.Equal(SupportedModels.All.Select(m => m.Name), SupportedModels.Known.Select(m => m.Name).Distinct(StringComparer.Ordinal));
        foreach (var choice in SupportedModels.All)
            Assert.All(SupportedModels.Versions(choice.Name), version =>
            {
                Assert.Equal(choice.Terms, version.Terms);
                Assert.Equal(choice.RequiredMemoryBytes, version.RequiredMemoryBytes);
            });
    }
}
