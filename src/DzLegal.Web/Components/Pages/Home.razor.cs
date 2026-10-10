using DzLegal.Presentation;
using DzLegal.Presentation.ViewModels;
using DzLegal.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DzLegal.Web.Components.Pages;

/// <summary>
/// The demo page: one session's view model (the same class the desktop window binds to), shown in three panels.
/// </summary>
public sealed partial class Home : IDisposable
{
    private ViewModelWatcher? _watcher;
    private bool showGuide = true;

    [Inject] private WebEngine Engine { get; set; } = default!;

    [Inject] private IJSRuntime Js { get; set; } = default!;

    [Inject] private IHttpContextAccessor Http { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "lang")] private string? LanguageQuery { get; set; }

    private MainWindowViewModel? Vm { get; set; }

    private Loc L => Vm!.L;

    private string Lang => Vm!.L.Language;

    protected override void OnInitialized()
    {
        // Judges read English; Arabic is one click away (the document itself is always Arabic).
        // The connection's address (after the proxy's X-Forwarded-For) keys the per-network limit.
        var address = Http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        Vm = Engine.CreateSession(new BrowserFiles(Js), new BrowserLinks(Js), LanguageQuery == "ar" ? "ar" : "en", address);
        _watcher = new ViewModelWatcher(Vm, RequestRender);
        if (Engine.Samples.Count > 0)
        {
            Open(Engine.Samples[0]);
        }
    }

    private string T(string key) => WebText.Get(key, Lang);

    private int _renderPending;
    private int _changes;
    private int _savedChanges;
    private Timer? _autosave;

    /// <summary>A case found in this browser's storage from an earlier visit, offered for restoring.</summary>
    private DzLegal.Domain.DraftDocument? SavedCase { get; set; }

    private DateTimeOffset? SavedAt { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            if (await Js.InvokeAsync<string?>("dzlegal.loadCase") is { } stored)
            {
                using var envelope = System.Text.Json.JsonDocument.Parse(stored);
                SavedAt = envelope.RootElement.GetProperty("savedAt").GetDateTimeOffset();
                SavedCase = System.Text.Json.JsonSerializer.Deserialize<DzLegal.Domain.DraftDocument>(
                    envelope.RootElement.GetProperty("json").GetString()!, DzLegal.Domain.Serialization.DefinitionJson.Options);
                StateHasChanged();
            }
        }
        catch (Exception ex) when (ex is JSException or System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            // A broken or old saved case is simply not offered.
        }

        // Saves every few seconds when something changed, in this browser only.
        _autosave = new Timer(_ => _ = InvokeAsync(SaveCaseAsync), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private async Task SaveCaseAsync()
    {
        if (Vm is null || SavedCase is not null || _changes == _savedChanges)
        {
            return;
        }

        _savedChanges = _changes;
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(Vm.SnapshotDraft(), DzLegal.Domain.Serialization.DefinitionJson.Options);
            await Js.InvokeVoidAsync("dzlegal.saveCase", json);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or TaskCanceledException)
        {
            // The tab is closing or storage is unavailable: nothing to do.
        }
    }

    private void RestoreSavedCase()
    {
        if (SavedCase is { } saved)
        {
            Vm!.RestoreDraft(saved);
        }

        SavedCase = null;
    }

    private async Task DiscardSavedCase()
    {
        SavedCase = null;
        await Js.InvokeVoidAsync("dzlegal.forgetCase");
    }

    /// <summary>
    /// Re-renders soon after a change, at most about 15 times a second: a streamed answer changes the view model on every
    /// token, and re-rendering the whole page each time would load the small demo server for nothing.
    /// </summary>
    private void RequestRender()
    {
        Interlocked.Increment(ref _changes);
        if (Interlocked.Exchange(ref _renderPending, 1) == 1)
        {
            return;
        }

        _ = InvokeAsync(async () =>
        {
            await Task.Delay(66);
            Volatile.Write(ref _renderPending, 0);
            StateHasChanged();
        });
    }

    private Task Run(CommunityToolkit.Mvvm.Input.IAsyncRelayCommand command) => CommandRunner.RunAsync(Vm!, command);

    // Preview pages as data URLs, encoded once per rendered page rather than on every re-render.
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<byte[], string> _pageSources = [];

    private string PageSource(byte[] page) =>
        _pageSources.GetValue(page, p => "data:image/png;base64," + Convert.ToBase64String(p));

    private void Open(SampleCase sample)
    {
        Vm!.OpenCase(sample.Inputs);
        Vm.StatusMessage = string.Format(L.Culture, T("SampleOpened"), sample.Title.Get(Lang));
    }

    private void OpenSample(ChangeEventArgs e)
    {
        if (Engine.Samples.FirstOrDefault(s => s.Id == e.Value as string) is { } sample)
        {
            Open(sample);
        }
    }

    private void SelectProvider(ChangeEventArgs e) =>
        Vm!.SelectedProvider = Vm.Providers.FirstOrDefault(p => p.Key == e.Value as string) ?? Vm.SelectedProvider;

    private void GoToStep(string id)
    {
        if (Vm!.FieldGroups.FirstOrDefault(g => g.Id == id) is { } step)
        {
            Vm.SelectedStep = step;
        }
    }

    private static string StepClass(FieldGroupViewModel step) =>
        step.IsMissing ? "missing" : step.IsWarning ? "warning" : step.IsDone ? "done" : "empty";

    public void Dispose()
    {
        _autosave?.Dispose();
        _watcher?.Dispose();
        Vm?.Dispose();
    }
}
