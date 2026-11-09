using DzLegal.App.Services.LocalModels;
using DzLegal.Drafting;

namespace DzLegal.App.Tests;

public class VerifiedProviderTests
{
    [Theory]
    [InlineData(ModelState.ModelMissing)]
    [InlineData(ModelState.MemoryLow)]
    [InlineData(ModelState.Refused)]
    public async Task AClickBeforeReadinessCannotCreateAProviderOrSendCaseText(ModelState state)
    {
        var created = 0;
        using var provider = new VerifiedLocalProvider(_ => Task.FromResult(new ModelStatus(state)),
            () => { created++; return new ScriptedProvider(); }, () => "local", new Loc());
        var error = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(new ModelRequest("test", "", "Fictitious notes", ""), CancellationToken.None));
        Assert.Equal(0, created);
        Assert.DoesNotContain("Fictitious notes", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerificationFinishesBeforeAnyGenerationAndOneWorkflowKeepsItsProvider()
    {
        var checkedOnce = false;
        var created = 0;
        using var provider = new VerifiedLocalProvider(_ => { checkedOnce = true; return Task.FromResult(new ModelStatus(ModelState.Ready)); },
            () => { Assert.True(checkedOnce); created++; return new ScriptedProvider(); }, () => "local", new Loc());
        var request = new ModelRequest("test", "", "Fictitious notes", "");
        await provider.GenerateAsync(request, CancellationToken.None);
        await provider.GenerateAsync(request, CancellationToken.None);
        Assert.Equal(1, created);
    }

    [Fact]
    public async Task SelectingTemplatesWhileAProbeWaitsDoesNotWarmOrPublishItsStaleResult()
    {
        var ready = new TaskCompletionSource<ModelStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var warmed = 0;
        var option = new ProviderOption("gated", _ => "gated", () => new ScriptedProvider(), _ => ready.Task,
            _ => { Interlocked.Increment(ref warmed); return Task.CompletedTask; });
        using var vm = Vm.Create(extra: option);
        var pending = vm.CheckModelStatusAsync();
        vm.SelectedProvider = vm.Providers.First(p => p.Key == "stub");
        ready.SetResult(new ModelStatus(ModelState.Ready, "stale"));
        await pending;
        Assert.Equal(0, warmed);
        Assert.Equal("stub", vm.SelectedProvider.Key);
        Assert.NotEqual("stale", vm.ModelStatus.Detail);
    }
}