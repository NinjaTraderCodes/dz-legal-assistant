using DzLegal.Presentation.Services;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Services.LocalModels;

public sealed partial class LocalModelSetupService
{
    public async Task<SetupModel> ReadPackAsync(string path, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        try { return (await new ModelPackStore(ModelsRoot, resources).ReadModelAsync(path, linked.Token).ConfigureAwait(false)).Info; }
        catch (ModelSetupException) { throw; }
        catch (OperationCanceledException) when (!linked.IsCancellationRequested)
        { throw new ModelSetupException(ModelSetupStage.EngineFailed); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { throw new ModelSetupException(ModelSetupStage.InvalidPack); }
    }

    public async Task ImportAsync(string path, string expectedModel, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        await acquisition.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            var model = await new ModelPackStore(ModelsRoot, resources).ImportAsync(path, expectedModel, progress, linked.Token).ConfigureAwait(false);
            progress.Report(new ModelSetupProgress(ModelSetupStage.Verifying, model.Name));
            if (!await verifier.VerifyAsync(model, Path.Combine(ModelsRoot, "blobs"), linked.Token, force: true).ConfigureAwait(false))
                throw new ModelSetupException(ModelSetupStage.VerificationFailed);
            var session = await engine.EnsureManagedAsync(linked.Token).ConfigureAwait(false);
            using var api = new LocalOllamaApi(session.Endpoint);
            if (!await api.IsLocalModelAsync(model.Name, linked.Token).ConfigureAwait(false))
                throw new ModelSetupException(ModelSetupStage.VerificationFailed);
            var ready = await CanUseAsync(session.Endpoint, model.Name, linked.Token).ConfigureAwait(false);
            if (ready == ModelState.ModelMissing) throw new ModelSetupException(ModelSetupStage.VerificationFailed);
            if (ready == ModelState.MemoryLow) { progress.Report(new ModelSetupProgress(ModelSetupStage.MemoryLow, model.Name, FilesVerified: true)); return; }
            progress.Report(new ModelSetupProgress(ModelSetupStage.Testing, model.Name));
            await SelfTestAsync(session.Endpoint, model.Name, linked.Token).ConfigureAwait(false);
            progress.Report(new ModelSetupProgress(ModelSetupStage.Ready, model.Name));
        }
        catch (ModelSetupException) { throw; }
        catch (OperationCanceledException) when (!linked.IsCancellationRequested)
        { throw new ModelSetupException(ModelSetupStage.EngineFailed); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { throw new ModelSetupException(ModelSetupStage.ImportFailed); }
        catch (Exception ex) when (ex is HttpRequestException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException or InvalidOperationException)
        { throw new ModelSetupException(ModelSetupStage.EngineFailed); }
        finally { acquisition.Release(); }
    }

    public async Task ExportAsync(string model, string path, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var versions = SupportedModels.Versions(model);
        if (versions.Count == 0) throw new ModelSetupException(ModelSetupStage.VerificationFailed);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        await acquisition.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            var session = await engine.EnsureAsync(linked.Token).ConfigureAwait(false);
            using var api = new LocalOllamaApi(session.Endpoint);
            var location = await api.LocationAsync(versions, linked.Token).ConfigureAwait(false);
            if (location is null || !await api.IsLocalModelAsync(model, linked.Token).ConfigureAwait(false))
                throw new ModelSetupException(ModelSetupStage.VerificationFailed);
            await new ModelPackStore(ModelsRoot, resources).ExportAsync(location.Value.Model, location.Value.BlobRoot, path, progress, linked.Token).ConfigureAwait(false);
            progress.Report(new ModelSetupProgress(ModelSetupStage.Exported, model, FilesVerified: true));
        }
        catch (ModelSetupException ex) when (ex.Stage == ModelSetupStage.InvalidPack)
        { throw new ModelSetupException(ModelSetupStage.VerificationFailed); }
        catch (ModelSetupException) { throw; }
        catch (OperationCanceledException) when (!linked.IsCancellationRequested)
        { throw new ModelSetupException(ModelSetupStage.EngineFailed); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException
            or HttpRequestException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException or InvalidOperationException)
        { throw new ModelSetupException(ModelSetupStage.ExportFailed); }
        finally { acquisition.Release(); }
    }
}