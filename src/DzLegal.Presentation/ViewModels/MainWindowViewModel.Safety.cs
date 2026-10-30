using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Presentation.Services;
using DzLegal.Storage;

namespace DzLegal.Presentation.ViewModels;

/// <summary>
/// Keeping the lawyer's work safe (design review of 22 Oct): New and Open ask before replacing unsaved work, the
/// recovery copy is never deleted by a click, the save state is visible, and a text longer than a field takes is kept
/// whole and pointed out instead of silently cut.
/// </summary>
public sealed partial class MainWindowViewModel
{
    private string _savedFingerprint = string.Empty;
    private string? _savedName;
    private DateTimeOffset? _savedAt;
    private ReplaceAction _pendingReplace;
    private string? _recoveryFile;

    private enum ReplaceAction
    {
        None,
        New,
        Open,
    }

    /// <summary>The case differs from the last file saved or opened (or from the empty case it started as).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSaveState))]
    public partial bool HasUnsavedChanges { get; private set; }

    /// <summary>«تعديلات غير محفوظة», or «محفوظة: الملف (14:32)» after a save or an open.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSaveState))]
    public partial string SaveStateText { get; private set; } = string.Empty;

    public bool HasSaveState => SaveStateText.Length > 0;

    /// <summary>New or Open waits for the lawyer: save first, continue without saving, or cancel.</summary>
    [ObservableProperty]
    public partial bool IsConfirmingReplace { get; private set; }

    [ObservableProperty]
    public partial string ReplacePrompt { get; private set; } = string.Empty;

    /// <summary>A draft exists and something still blocks its export: the list shows before any attempt to export.</summary>
    public bool ShowsExportBlockers => HasExportBlockers && (HasGeneratedDraft || IsExportBlockedShown);

    /// <summary>Before an attempt: «قبل التصدير، بقي:»; after a refused one: «لا يُصدَّر المستند قبل إتمام ما يلي:».</summary>
    public string ExportBlockersTitle => IsExportBlockedShown ? L["ExportBlocked"] : L["BeforeExport"];

    partial void OnExportBlockersChanged(string value) => NotifyBlockersShown();

    partial void OnIsExportBlockedShownChanged(bool value) => NotifyBlockersShown();

    partial void OnHasGeneratedDraftChanged(bool value) => NotifyBlockersShown();

    private void NotifyBlockersShown()
    {
        OnPropertyChanged(nameof(ShowsExportBlockers));
        OnPropertyChanged(nameof(ExportBlockersTitle));
    }

    [RelayCommand]
    private void New()
    {
        if (!AskBeforeReplacing(ReplaceAction.New))
        {
            StartNew();
        }
    }

    [RelayCommand]
    private async Task OpenAsync() => await RequestOpenAsync(null);

    [RelayCommand]
    private async Task SaveAsync() => await SaveFileAsync();

    /// <summary>«احفظ أولا»: saves, then does what was asked; a cancelled save leaves everything as it was.</summary>
    [RelayCommand]
    private async Task SaveThenReplaceAsync()
    {
        if (await SaveFileAsync())
        {
            await ReplaceAsync();
        }
    }

    /// <summary>«تابع دون حفظ»: the lawyer chose to leave the unsaved changes; the autosave copy still holds them until replaced.</summary>
    [RelayCommand]
    private async Task ReplaceWithoutSavingAsync() => await ReplaceAsync();

    [RelayCommand]
    private void CancelReplace()
    {
        _pendingReplace = ReplaceAction.None;
        _pendingOpenPath = null;
        IsConfirmingReplace = false;
    }

    private bool AskBeforeReplacing(ReplaceAction action)
    {
        UpdateSaveState();
        if (!HasUnsavedChanges)
        {
            return false;
        }

        _pendingReplace = action;
        _pendingOpenPath = null;
        ReplacePrompt = L[action == ReplaceAction.New ? "UnsavedBeforeNew" : "UnsavedBeforeOpen"];
        IsConfirmingReplace = true;
        return true;
    }

    private async Task ReplaceAsync()
    {
        var action = _pendingReplace;
        var path = _pendingOpenPath;
        CancelReplace();
        if (action == ReplaceAction.New)
        {
            StartNew();
        }
        else if (action == ReplaceAction.Open)
        {
            await OpenFileAsync(path);
        }
    }

    /// <summary>A cancelled or failed save keeps the pending replacement and the unsaved case.</summary>
    private async Task<bool> SaveFileAsync()
    {
        try
        {
            var target = await _files.SaveAsync(SuggestedFileName() + DraftFile.Extension, SaveKind.Draft);
            if (target is not { } t) return false;

            await using (t.Stream)
            {
                await DraftFile.SaveAsync(CurrentDraft(), t.Stream);
            }

            MarkSaved(t.Name);
            RecordRecentCase(saved: true);
            SetStatus(L.Format("Saved", t.Name));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            SetStatus(L.Format("Error", ex.Message), isError: true);
            return false;
        }
    }

    /// <summary>The current case is the one on disk (just saved or opened), or the empty case of a new start.</summary>
    private void MarkSaved(string? name)
    {
        _savedFingerprint = SaveFingerprint();
        _savedName = name;
        _savedAt = name is null ? null : DateTimeOffset.Now;
        UpdateSaveState();
    }

    /// <summary>Everything a save keeps: the inputs, the sections and their review, the selected articles.</summary>
    private string SaveFingerprint() =>
        string.Join('\u0001', CurrentInputs().OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + "=" + kv.Value))
        + '\u0002' + string.Join('\u0001', Sections.Select(s => s.Id + "=" + s.Text + (s.IsReviewed ? "✓" : string.Empty)))
        + '\u0002' + string.Join(',', _selectedArticles)
        + '\u0002' + (_analysis?.ClaimConfirmed == true ? "confirmed" : string.Empty);

    private void UpdateSaveState()
    {
        HasUnsavedChanges = SaveFingerprint() != _savedFingerprint;
        SaveStateText = HasUnsavedChanges ? L["UnsavedChanges"]
            : _savedName is not null && _savedAt is { } at ? L.Format("SavedState", _savedName, at.ToString("HH:mm", L.Culture))
            : string.Empty;
    }

    /// <summary>
    /// Moves the previous session's autosave aside (once) so that this session's autosave never overwrites it before the
    /// lawyer has chosen. Restoring reads it from there.
    /// </summary>
    private void KeepRecoveryAside()
    {
        if (_autosaveFile is null || _recoveryFile != _autosaveFile || !File.Exists(_autosaveFile))
        {
            return;
        }

        var aside = Path.Combine(Path.GetDirectoryName(_autosaveFile)!, "previous-session" + DraftFile.Extension);
        try
        {
            File.Move(_autosaveFile, aside, overwrite: true);
            _recoveryFile = aside;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left in place: this session's autosave then waits until the lawyer chooses.
        }
    }

    /// <summary>The fields whose text is longer than they take, with their labels.</summary>
    private List<FieldViewModel> FieldsTooLong() => _fields.Where(f => f.IsTooLong).ToList();

    /// <summary>An AI step waits until over-long fields are shortened: the model must read the whole text the lawyer wrote.</summary>
    private bool RefuseWhileTooLong()
    {
        var tooLong = FieldsTooLong();
        if (tooLong.Count == 0)
        {
            return false;
        }

        SetStatus(L.Format("ShortenFieldsFirst", string.Join("، ", tooLong.Select(f => f.Label))), isError: true);
        return true;
    }
}
