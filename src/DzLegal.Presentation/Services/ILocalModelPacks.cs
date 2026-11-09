namespace DzLegal.Presentation.Services;

public interface ILocalModelPacks
{
    Task<SetupModel> ReadPackAsync(string path, CancellationToken cancellationToken);
    Task ImportAsync(string path, string expectedModel, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken);
    Task ExportAsync(string model, string path, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken);
}

public interface IModelPackFiles
{
    Task<string?> PickImportAsync();
    Task<string?> PickExportAsync(string model);
}