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

    [SupplyParameterFromQuery(Name = "lang")] private string? LanguageQuery { get; set; }

    private MainWindowViewModel? Vm { get; set; }

    private Loc L => Vm!.L;

    private string Lang => Vm!.L.Language;

    protected override void OnInitialized()
    {
        // Judges read English; Arabic is one click away (the document itself is always Arabic).
        Vm = Engine.CreateSession(new BrowserFiles(Js), new BrowserLinks(Js), LanguageQuery == "ar" ? "ar" : "en");
        _watcher = new ViewModelWatcher(Vm, () => _ = InvokeAsync(StateHasChanged));
        if (Engine.Samples.Count > 0)
        {
            Open(Engine.Samples[0]);
        }
    }

    private string T(string key) => WebText.Get(key, Lang);

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
        _watcher?.Dispose();
        Vm?.Dispose();
    }
}
