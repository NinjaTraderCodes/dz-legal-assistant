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
            if (request.RequestUri!.AbsolutePath == "/api/show")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"details\":{\"format\":\"gguf\"},\"model_info\":{\"general.architecture\":\"gemma3\"}}", Encoding.UTF8, "application/json") });
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

    [Theory]
    [InlineData("gemma4:e4b", null, false)]
    [InlineData("gemma4:e2b", null, false)]
    [InlineData("GEMMA4", null, false)]
    [InlineData("gemma4:e4b", true, true)]
    [InlineData("gemma3:4b", null, null)]
    [InlineData("dz-gemma3-4b-text", null, null)]
    [InlineData("qwen3:4b", false, false)]
    [InlineData("gemma40-like:1b", null, null)]
    public void Gemma4RunsWithThinkingOffUnlessTheSettingsSayOtherwise(string model, bool? configured, bool? sent) =>
        Assert.Equal(sent, OllamaModelChoice.ThinkFor(model, configured));

    [Fact]
    public void TheShippedDefaultModelIsUnchangedAndSendsNoThinkingSwitch()
    {
        var settings = new OllamaSettings();

        Assert.Equal("gemma3:4b", settings.Model);
        Assert.Null(OllamaModelChoice.ThinkFor(settings.Model, settings.Think));
    }

    [Fact]
    public async Task FallingBackFromGemma4ToGemma3DoesNotSendAThinkingSwitch()
    {
        var choice = new OllamaModelChoice(new OllamaOptions { Model = "gemma4:e4b" }, null);
        Assert.False(choice.Options.Think);
        Assert.Equal(ModelState.Ready, (await choice.ProbeAsync(CancellationToken.None, new TagsHandler("gemma3:4b"))).State);
        Assert.Equal("gemma3:4b", choice.Options.Model);
        Assert.Null(choice.Options.Think);
    }

    [Fact]
    public async Task AnExplicitAdvancedModelKeepsItsOwnThinkingSetting()
    {
        var choice = new OllamaModelChoice(new OllamaOptions { Model = "qwen3:4b", Think = false }, null);
        Assert.Equal(ModelState.Ready, (await choice.ProbeAsync(CancellationToken.None, new TagsHandler("gemma4:e4b", "qwen3:4b"))).State);
        Assert.Equal("qwen3:4b", choice.Options.Model);
        Assert.False(choice.Options.Think);
    }
}
