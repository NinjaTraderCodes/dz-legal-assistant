using DzLegal.App.Services;
using DzLegal.App.ViewModels;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.ModelProviders.Ollama;
using DzLegal.ModelProviders.Stub;
using DzLegal.Storage;

namespace DzLegal.App;

/// <summary>Wires the application together. Everything is local: data folder, settings, storage, model.</summary>
public static class Composition
{
    public static string DataRoot => Path.Combine(AppContext.BaseDirectory, "data", "document-types");

    public static IReadOnlyList<ProviderOption> CreateProviders(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var ollama = new OllamaOptions
        {
            Endpoint = new Uri(settings.Ollama.Endpoint),
            Model = settings.Ollama.Model,
            Timeout = TimeSpan.FromMinutes(settings.Ollama.TimeoutMinutes),
            Temperature = settings.Ollama.Temperature,
            MaxTokens = settings.Ollama.MaxTokens,
            RepeatPenalty = settings.Ollama.RepeatPenalty,
            Think = settings.Ollama.Think,
        };

        return
        [
            new ProviderOption("ollama", loc => loc.Format("ProviderOllama", ollama.Model), () => new OllamaModelProvider(ollama)),
            new ProviderOption("stub", loc => loc["ProviderStub"], () => new StubModelProvider()),
        ];
    }

    public static MainWindowViewModel CreateMainViewModel(AppSettings settings, IUserFiles files, AppPaths paths, Loc loc)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(loc);
        loc.SetLanguage(settings.Language);

        // Target 1 ships one document type; the loader already supports more.
        var definition = DefinitionLoader.Load(Path.Combine(DataRoot, "requete-introductive"));
        return new MainWindowViewModel(
            definition,
            CreateProviders(settings),
            files,
            new EvaluationStore(paths.Evaluations),
            loc,
            settings.DefaultProvider);
    }
}
