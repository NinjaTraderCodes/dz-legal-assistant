using System.Net;
using System.Text;
using DzLegal.App;
using DzLegal.ModelProviders.Ollama;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Tests;

public class OllamaModelChoiceTests
{
    private sealed class TagsHandler(params string[] models) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = "{\"models\":[" + string.Join(',', models.Select(m => $"{{\"name\":\"{m}\"}}")) + "]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public async Task TheTextOnlyModelIsUsedWhenInstalled()
    {
        var choice = new OllamaModelChoice(new OllamaOptions { Model = "gemma3:4b" }, "dz-gemma3-4b-text");

        var status = await choice.ProbeAsync(CancellationToken.None, new TagsHandler("gemma3:4b", "dz-gemma3-4b-text:latest"));

        Assert.Equal(ModelState.Ready, status.State);
        Assert.Equal("dz-gemma3-4b-text", choice.Options.Model);
    }

    [Fact]
    public async Task WithoutTheTextOnlyModelTheConfiguredOneIsUsed()
    {
        var choice = new OllamaModelChoice(new OllamaOptions { Model = "gemma3:4b" }, "dz-gemma3-4b-text");

        var status = await choice.ProbeAsync(CancellationToken.None, new TagsHandler("gemma3:4b"));

        Assert.Equal(ModelState.Ready, status.State);
        Assert.Equal("gemma3:4b", choice.Options.Model);
        Assert.Equal(ModelState.ModelMissing, (await new OllamaModelChoice(new OllamaOptions { Model = "gemma3:4b" }, "dz-gemma3-4b-text")
            .ProbeAsync(CancellationToken.None, new TagsHandler("qwen2.5:7b-instruct"))).State);
    }

    [Fact]
    public void AnotherConfiguredModelIsNeverReplaced()
    {
        Assert.Equal("dz-gemma3-4b-text", new AppSettings().Ollama.TextOnlyModel);
        Assert.Equal("dz-gemma3-4b-text", OllamaModelChoice.TextOnlyModelFor("gemma3:4b", "dz-gemma3-4b-text"));
        Assert.Null(OllamaModelChoice.TextOnlyModelFor("qwen2.5:7b-instruct", "dz-gemma3-4b-text"));
    }
}
