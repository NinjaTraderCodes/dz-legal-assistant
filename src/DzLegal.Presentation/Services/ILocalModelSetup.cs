namespace DzLegal.Presentation.Services;

public enum ModelSetupStage
{
    Checking, Available, Preparing, Downloading, Cancelled, Verifying, Testing, Ready,
    ConnectionFailed, DiskFull, MemoryLow, VerificationFailed, EngineFailed,
    ImportSelected, Importing, Exporting, Exported, InvalidPack, PackTooLarge, ImportFailed, ExportFailed,
}

public sealed record SetupModel(string Name, long Bytes, string Terms, long RequiredMemoryBytes);
public sealed record ModelSetupProgress(ModelSetupStage Stage, string Model, long Completed = 0, long Total = 0,
    double BytesPerSecond = 0, long FreeDiskBytes = 0, long RequiredDiskBytes = 0);

public interface ILocalModelSetup
{
    IReadOnlyList<SetupModel> Models { get; }
    Task<ModelSetupProgress> InspectAsync(CancellationToken cancellationToken);
    Task<ModelSetupProgress> InspectModelAsync(string model, CancellationToken cancellationToken);
    Task DownloadAsync(string model, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken);
    void RememberTemplates();
    void RememberLocal();
    bool PreferTemplates { get; }
}

public sealed class ModelSetupException(ModelSetupStage stage) : IOException("Local model setup failed.")
{
    public ModelSetupStage Stage { get; } = stage;
}