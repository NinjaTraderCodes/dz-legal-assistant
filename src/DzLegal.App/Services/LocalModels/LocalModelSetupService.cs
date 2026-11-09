using System.Net.Http.Json;
using System.Text.Json;
using DzLegal.Presentation.Services;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Services.LocalModels;

public sealed partial class LocalModelSetupService : ILocalModelSetup, ILocalModelPacks, IDisposable
{
    private static readonly string[] Preference = ["gemma4:e4b", "gemma3:4b"];
    public const long DiskReserveBytes = 2L * 1024 * 1024 * 1024;
    private readonly string root;
    private readonly ManagedOllama engine;
    private readonly IComputerResources resources;
    private readonly ModelVerifier verifier = new();
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim acquisition = new(1, 1);
    private int disposed;
    private string ModelsRoot => Path.Combine(root, "models");
    private string PreferencesPath => Path.Combine(root, "model-setup.json");

    public LocalModelSetupService(string root, ManagedOllama engine, IComputerResources? resources = null)
    {
        this.root = root;
        this.engine = engine;
        this.resources = resources ?? new WindowsComputerResources();
        try
        {
            if (File.Exists(PreferencesPath))
                PreferTemplates = JsonSerializer.Deserialize<bool>(File.ReadAllText(PreferencesPath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }
    public IReadOnlyList<SetupModel> Models => SupportedModels.All.Select(m => m.Info).ToArray();
    public bool PreferTemplates { get; private set; }

    public void RememberTemplates()
    {
        PreferTemplates = true;
        try { Directory.CreateDirectory(root); File.WriteAllText(PreferencesPath, "true"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    public void RememberLocal()
    {
        PreferTemplates = false;
        try { Directory.CreateDirectory(root); File.WriteAllText(PreferencesPath, "false"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    public async Task<ModelState> CanUseAsync(Uri endpoint, string name, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        using var api = new LocalOllamaApi(endpoint);
        var supported = SupportedModels.All.FirstOrDefault(m => m.Name == name);
        if (supported is not null)
        {
            var location = await api.LocationAsync(supported, linked.Token).ConfigureAwait(false);
            if (location is null || !await verifier.VerifyAsync(supported, location.Value.BlobRoot, linked.Token).ConfigureAwait(false))
                return ModelState.ModelMissing;
        }
        if (resources.AvailableMemoryBytes < 512UL * 1024 * 1024) return ModelState.MemoryLow;
        if (supported is not null && resources.AvailableMemoryBytes < (ulong)(supported.RequiredMemoryBytes + 512L * 1024 * 1024)
            && !await api.IsLoadedAsync(name, linked.Token).ConfigureAwait(false)) return ModelState.MemoryLow;
        return ModelState.Ready;
    }

    public Task<ModelSetupProgress> InspectAsync(CancellationToken cancellationToken) => InspectCoreAsync(null, cancellationToken);
    public Task<ModelSetupProgress> InspectModelAsync(string model, CancellationToken cancellationToken)
    {
        _ = SupportedModels.Find(model);
        return InspectCoreAsync(model, cancellationToken);
    }

    private async Task<ModelSetupProgress> InspectCoreAsync(string? selected, CancellationToken cancellationToken)
    {
        var recommended = selected ?? WindowsComputerResources.RecommendedModel(resources.InstalledMemoryBytes);
        try
        {
            var session = await engine.EnsureAsync(cancellationToken).ConfigureAwait(false);
            using var api = new LocalOllamaApi(session.Endpoint);
            var installed = await api.InstalledAsync(cancellationToken).ConfigureAwait(false);
            string? memoryLow = null;
            var verificationFailed = false;
            var candidates = selected is null ? Preference : [selected];
            foreach (var name in candidates.Where(installed.Contains))
            {
                var state = await CanUseAsync(session.Endpoint, name, cancellationToken).ConfigureAwait(false);
                if (state == ModelState.Ready) return new ModelSetupProgress(ModelSetupStage.Ready, name);
                if (state == ModelState.MemoryLow) memoryLow ??= name;
                verificationFailed |= state == ModelState.ModelMissing;
            }
            if (memoryLow is not null) return new ModelSetupProgress(ModelSetupStage.MemoryLow, memoryLow, FilesVerified: true);
            if (verificationFailed) return new ModelSetupProgress(ModelSetupStage.VerificationFailed, recommended);
            var model = SupportedModels.Find(recommended);
            var free = resources.FreeDiskBytes(root);
            var required = RequiredDisk(model);
            return new ModelSetupProgress(free < required ? ModelSetupStage.DiskFull : ModelSetupStage.Available, recommended,
                FreeDiskBytes: free, RequiredDiskBytes: required);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return new ModelSetupProgress(ModelSetupStage.EngineFailed, recommended); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception
            or HttpRequestException or JsonException or InvalidOperationException)
        { return new ModelSetupProgress(ModelSetupStage.EngineFailed, recommended); }
    }

    private long RequiredDisk(SupportedModel model) => DiskReserveBytes + model.Blobs.Sum(b =>
    {
        var path = Path.Combine(ModelsRoot, "blobs", "sha256-" + b.Sha256);
        return File.Exists(path) && new FileInfo(path).Length == b.Size ? 0
            : Math.Max(0, b.Size - resources.AllocatedFileBytes(path + "-partial"));
    });

    public async Task DownloadAsync(string model, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var supported = SupportedModels.Find(model);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        await acquisition.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(root);
            var current = await engine.EnsureAsync(linked.Token).ConfigureAwait(false);
            using (var existingApi = new LocalOllamaApi(current.Endpoint))
            {
                if ((await existingApi.InstalledAsync(linked.Token).ConfigureAwait(false)).Contains(model))
                {
                    var existing = await CanUseAsync(current.Endpoint, model, linked.Token).ConfigureAwait(false);
                    if (existing == ModelState.Ready) { progress.Report(new ModelSetupProgress(ModelSetupStage.Ready, model)); return; }
                    if (existing == ModelState.MemoryLow) throw new ModelSetupException(ModelSetupStage.MemoryLow);
                }
            }
            foreach (var blob in supported.Blobs)
            {
                var path = Path.Combine(ModelsRoot, "blobs", "sha256-" + blob.Sha256);
                if (!File.Exists(path)) continue;
                bool valid;
                await using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous))
                    valid = input.Length == blob.Size && Convert.ToHexStringLower(await System.Security.Cryptography.SHA256.HashDataAsync(input, linked.Token).ConfigureAwait(false)) == blob.Sha256;
                if (!valid) File.Delete(path); // only an invalid supported blob in our private cache, on explicit retry
            }
            var free = resources.FreeDiskBytes(root);
            var required = RequiredDisk(supported);
            if (free < required) throw new ModelSetupException(ModelSetupStage.DiskFull);
            progress.Report(new ModelSetupProgress(ModelSetupStage.Preparing, model));
            var session = await engine.EnsureManagedAsync(linked.Token).ConfigureAwait(false);
            using var client = new HttpClient(DzLegal.ModelProviders.Ollama.LoopbackGuard.CreateHandler()) { BaseAddress = session.Endpoint, Timeout = Timeout.InfiniteTimeSpan };
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/pull")
            {
                Content = JsonContent.Create(new { model, runner = "llamacpp", stream = true }),
            };
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, linked.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new ModelSetupException(ModelSetupStage.ConnectionFailed);
            await ReadPullAsync(await response.Content.ReadAsStreamAsync(linked.Token).ConfigureAwait(false), supported, progress,
                () =>
                {
                    if (resources.FreeDiskBytes(root) < DiskReserveBytes) throw new ModelSetupException(ModelSetupStage.DiskFull);
                    if (resources.AvailableMemoryBytes < 512UL * 1024 * 1024) throw new ModelSetupException(ModelSetupStage.MemoryLow);
                }, linked.Token).ConfigureAwait(false);
            progress.Report(new ModelSetupProgress(ModelSetupStage.Verifying, model));
            if (!await verifier.VerifyAsync(supported, Path.Combine(ModelsRoot, "blobs"), linked.Token, force: true).ConfigureAwait(false))
                throw new ModelSetupException(ModelSetupStage.VerificationFailed);
            await ModelVerifier.PublishManifestAsync(supported, ModelsRoot, linked.Token).ConfigureAwait(false);
            progress.Report(new ModelSetupProgress(ModelSetupStage.Testing, model));
            using var api = new LocalOllamaApi(session.Endpoint);
            if (!await api.IsLocalModelAsync(model, linked.Token).ConfigureAwait(false))
                throw new ModelSetupException(ModelSetupStage.VerificationFailed);
            var state = await CanUseAsync(session.Endpoint, model, linked.Token).ConfigureAwait(false);
            if (state == ModelState.ModelMissing) throw new ModelSetupException(ModelSetupStage.VerificationFailed);
            if (state == ModelState.MemoryLow) { progress.Report(new ModelSetupProgress(ModelSetupStage.MemoryLow, model, FilesVerified: true)); return; }
            await SelfTestAsync(session.Endpoint, model, linked.Token).ConfigureAwait(false);
            progress.Report(new ModelSetupProgress(ModelSetupStage.Ready, model));
        }
        catch (ModelSetupException) { throw; }
        catch (OperationCanceledException) when (!linked.IsCancellationRequested) { throw new ModelSetupException(ModelSetupStage.ConnectionFailed); }
        catch (HttpRequestException) { throw new ModelSetupException(ModelSetupStage.ConnectionFailed); }
        catch (JsonException) { throw new ModelSetupException(ModelSetupStage.VerificationFailed); }
        catch (IOException) { throw new ModelSetupException(resources.FreeDiskBytes(root) < DiskReserveBytes ? ModelSetupStage.DiskFull : ModelSetupStage.EngineFailed); }
        finally { acquisition.Release(); }
    }

    internal static async Task ReadPullAsync(Stream input, SupportedModel supported, IProgress<ModelSetupProgress> progress,
        Action guard, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(input);
        var parser = new PullProgress(supported);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (line.Length == 0) continue;
            guard();
            progress.Report(parser.Parse(line));
        }
        if (!parser.Success) throw new ModelSetupException(ModelSetupStage.ConnectionFailed);
    }

    private static async Task SelfTestAsync(Uri endpoint, string model, CancellationToken cancellationToken)
    {
        using var client = new HttpClient(DzLegal.ModelProviders.Ollama.LoopbackGuard.CreateHandler()) { Timeout = TimeSpan.FromMinutes(3) };
        var body = new Dictionary<string, object>
        {
            ["model"] = model, ["prompt"] = "اكتب كلمة جاهز فقط.", ["stream"] = false,
            ["keep_alive"] = "2m", ["options"] = new { num_ctx = 4096, num_gpu = 0, num_predict = 32, temperature = 0 },
        };
        if (OllamaModelChoice.IsGemma4(model)) body["think"] = false;
        using var response = await client.PostAsJsonAsync(new Uri(endpoint, "/api/generate"), body, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = json.RootElement;
        if (!root.TryGetProperty("done", out var done) || !done.GetBoolean()
            || !root.TryGetProperty("response", out var answer) || string.IsNullOrWhiteSpace(answer.GetString())
            || root.TryGetProperty("done_reason", out var reason) && reason.GetString() == "length")
            throw new ModelSetupException(ModelSetupStage.VerificationFailed);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        lifetime.Cancel();
        verifier.Dispose();
        lifetime.Dispose();
    }
}