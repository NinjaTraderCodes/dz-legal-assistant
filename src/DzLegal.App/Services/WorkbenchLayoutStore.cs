using System.Text.Json;
using System.Text.Json.Serialization;

namespace DzLegal.App.Services;

/// <summary>Window preferences only: never stored in a case, recovery copy, or legal document.</summary>
public sealed record WorkbenchLayout
{
    public double OutlineWidth { get; init; } = 224;
    public double EditorWidth { get; init; } = 440;
    public double ToolsHeight { get; init; } = 240;
    public bool? OutlineCompact { get; init; }
    public bool OutlineVisible { get; init; } = true;
    public bool ToolsOpen { get; init; }
    public string Tool { get; init; } = "review";

    [JsonIgnore]
    public bool IsValid => double.IsFinite(OutlineWidth) && OutlineWidth is >= 160 and <= 360
        && double.IsFinite(EditorWidth) && EditorWidth is >= 320 and <= 900
        && double.IsFinite(ToolsHeight) && ToolsHeight is >= 120 and <= 500
        && Tool is "review" or "source" or "assistant" or "activity";
}

public sealed class WorkbenchLayoutStore(string file)
{
    public WorkbenchLayout Load()
    {
        try
        {
            if (File.Exists(file) && new FileInfo(file).Length <= 16384
                && JsonSerializer.Deserialize<WorkbenchLayout>(File.ReadAllText(file)) is { IsValid: true } layout)
            {
                return layout;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Preferences are optional; an unavailable or malformed file must not interrupt a case.
        }

        return new WorkbenchLayout();
    }

    public void Save(WorkbenchLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!layout.IsValid)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
            var temporary = file + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(layout));
            File.Move(temporary, file, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A layout write failure never changes the saved-state indicator of the case.
        }
    }
}
