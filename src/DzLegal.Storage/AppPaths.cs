namespace DzLegal.Storage;

/// <summary>Local folders used by the application. Nothing is ever stored outside this machine.</summary>
public sealed class AppPaths
{
    public AppPaths(string root)
    {
        Root = root;
    }

    /// <summary>Per-user local application data, e.g. %LOCALAPPDATA%\DzLegalAssistant on Windows.</summary>
    public static AppPaths Default { get; } = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        "DzLegalAssistant"));

    public string Root { get; }

    public string Drafts => Ensure(Path.Combine(Root, "drafts"));

    public string Evaluations => Ensure(Path.Combine(Root, "evaluations"));

    private static string Ensure(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
