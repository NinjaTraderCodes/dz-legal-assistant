using DzLegal.App.Services;
using DzLegal.Domain;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public sealed class RecentCaseIntegrationTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-guide-recents-");
    private RecentCasesStore Store => new(_folder.FullName);
    private string CasePath => Path.Combine(_folder.FullName, "fictional" + DraftFile.Extension);

    [Theory]
    [InlineData("Fictitious claim", "Fictitious claim")]
    [InlineData("", "Fictitious Plaintiff")]
    public async Task SuccessfulSaveAndPickerOpenRecordTheFullPathAndSubject(string subject, string expected)
    {
        var files = new LocalFiles { SavePath = CasePath, OpenPath = CasePath };
        using var model = Vm.Create(files, recentCases: Store);
        model.Field("claim.subject").Value = subject;
        model.Field("plaintiff.lastName").Value = "Fictitious";
        model.Field("plaintiff.firstName").Value = "Plaintiff";
        await model.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(Store.Load());
        Assert.Equal(CasePath, saved.Path);
        Assert.Equal(expected, saved.Subject);
        Assert.False(model.HasUnsavedChanges);

        Store.Clear();
        using var reopened = Vm.Create(files, recentCases: Store);
        await reopened.OpenCommand.ExecuteAsync(null);
        var opened = Assert.Single(Store.Load());
        Assert.Equal(CasePath, opened.Path);
        Assert.Equal(expected, opened.Subject);
        Assert.Equal(subject, reopened.Field("claim.subject").Value);
    }

    [Fact]
    public async Task CancelledPickersFailedWritesAndUnreadableFilesRecordNothing()
    {
        var files = new LocalFiles();
        using var model = Vm.Create(files, recentCases: Store);
        model.Field("notes.facts").Value = "Fictitious unsaved facts";
        await model.SaveCommand.ExecuteAsync(null);
        Assert.Empty(Store.Load());
        Assert.True(model.HasUnsavedChanges);
        using var empty = Vm.Create(files, recentCases: Store);
        await empty.OpenCommand.ExecuteAsync(null);
        Assert.Empty(Store.Load());

        files.SavePath = CasePath;
        files.FailWrite = true;
        await model.SaveCommand.ExecuteAsync(null);
        Assert.Empty(Store.Load());
        Assert.True(model.HasUnsavedChanges);
        Assert.True(model.IsError);

        await File.WriteAllTextAsync(CasePath, "{unreadable");
        files.OpenPath = CasePath;
        await empty.OpenCommand.ExecuteAsync(null);
        Assert.Empty(Store.Load());
        Assert.True(empty.IsError);
        Assert.Equal(string.Empty, empty.Field("notes.facts").Value);
    }

    [Fact]
    public async Task PdfAndWordExportsAndRecoveryRestoreDoNotRecordCases()
    {
        var files = new LocalFiles { SavePath = CasePath };
        using var model = Vm.Create(files, recentCases: Store).Filled();
        await model.GenerateCommand.ExecuteAsync(null);
        foreach (var section in model.Sections) section.IsReviewed = true;
        await model.ExportPdfCommand.ExecuteAsync(null);
        await model.ExportWordCommand.ExecuteAsync(null);
        Assert.Contains(SaveKind.Pdf, files.SavedKinds);
        Assert.Contains(SaveKind.Word, files.SavedKinds);
        Assert.Empty(Store.Load());

        var recovery = Path.Combine(_folder.FullName, "recovery" + DraftFile.Extension);
        using (var previous = Vm.Create(autosaveFile: recovery).Filled()) previous.AutosaveNow();
        using var restored = Vm.Create(files, autosaveFile: recovery, recentCases: Store);
        await restored.RestoreLastSessionCommand.ExecuteAsync(null);
        Assert.Equal(model.Field("claim.subject").Value, restored.Field("claim.subject").Value);
        Assert.Empty(Store.Load());
        Assert.True(File.Exists(recovery));
    }

    [Fact]
    public async Task RecentOpenWaitsForTheExistingReplaceDecisionAndCancelChangesNothing()
    {
        await WriteCaseAsync(CasePath, "Fictitious incoming");
        var files = new LocalFiles { OpenPath = CasePath };
        using var model = Vm.Create(files, recentCases: Store);
        model.Field("notes.facts").Value = "Fictitious unsaved facts";
        var sectionTexts = model.Sections.Select(section => section.Text).ToArray();
        await model.OpenRecentCaseAsync(CasePath);
        Assert.True(model.IsConfirmingReplace);
        Assert.Equal(model.L["UnsavedBeforeOpen"], model.ReplacePrompt);
        Assert.Equal(0, files.PathReads);
        Assert.Empty(Store.Load());
        model.CancelReplaceCommand.Execute(null);
        Assert.False(model.IsConfirmingReplace);
        Assert.True(model.HasUnsavedChanges);
        Assert.Equal("Fictitious unsaved facts", model.Field("notes.facts").Value);
        Assert.Equal(sectionTexts, model.Sections.Select(section => section.Text));
        Assert.Equal(0, files.PathReads);

        await model.OpenCommand.ExecuteAsync(null);
        await model.ReplaceWithoutSavingCommand.ExecuteAsync(null);
        Assert.Equal(1, files.PickerReads);
        Assert.Equal(0, files.PathReads);
        Assert.Equal("Fictitious incoming", Assert.Single(Store.Load()).Subject);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecentOpenUsesTheOriginalSaveFirstAndContinueWithoutSavingPaths(bool saveFirst)
    {
        var incoming = Path.Combine(_folder.FullName, "incoming" + DraftFile.Extension);
        await WriteCaseAsync(incoming, "Fictitious incoming");
        var files = new LocalFiles { SavePath = CasePath };
        using var model = Vm.Create(files, recentCases: Store);
        model.Field("claim.subject").Value = "Fictitious outgoing";
        await model.OpenRecentCaseAsync(incoming);
        if (saveFirst) await model.SaveThenReplaceCommand.ExecuteAsync(null);
        else await model.ReplaceWithoutSavingCommand.ExecuteAsync(null);
        Assert.False(model.IsConfirmingReplace);
        Assert.False(model.HasUnsavedChanges);
        Assert.Equal(1, files.PathReads);
        Assert.Equal("Fictitious incoming", model.Field("claim.subject").Value);
        Assert.Equal(incoming, Store.Load()[0].Path);
        Assert.Equal(saveFirst ? 2 : 1, Store.Load().Count);
        Assert.Equal(saveFirst, File.Exists(CasePath));
        if (saveFirst) Assert.Equal("Fictitious outgoing", (await DraftFile.LoadAsync(CasePath)).Inputs["claim.subject"]);
    }

    [Fact]
    public async Task CancelledOrFailedSaveFirstDoesNotOpenTheRecentCase()
    {
        await WriteCaseAsync(CasePath, "Fictitious incoming");
        var files = new LocalFiles();
        using var model = Vm.Create(files, recentCases: Store);
        model.Field("notes.facts").Value = "Fictitious unsaved facts";
        await model.OpenRecentCaseAsync(CasePath);
        await model.SaveThenReplaceCommand.ExecuteAsync(null);
        Assert.True(model.IsConfirmingReplace);
        Assert.Equal(0, files.PathReads);
        files.SavePath = Path.Combine(_folder.FullName, "failed" + DraftFile.Extension);
        files.FailWrite = true;
        await model.SaveThenReplaceCommand.ExecuteAsync(null);
        Assert.True(model.IsConfirmingReplace);
        Assert.Equal(0, files.PathReads);
        Assert.Equal("Fictitious unsaved facts", model.Field("notes.facts").Value);
        Assert.Empty(Store.Load());
        model.CancelReplaceCommand.Execute(null);
    }

    [Fact]
    public async Task PickerAndRecentOpenUseTheSameTypeCheckAndUnreadableFileMessage()
    {
        await DraftFile.SaveAsync(new DraftDocument { DocumentTypeId = "fictitious-other-type" }, CasePath);
        var files = new LocalFiles { OpenPath = CasePath };
        using var picker = Vm.Create(files, recentCases: Store);
        using var recent = Vm.Create(files, recentCases: Store);
        await picker.OpenCommand.ExecuteAsync(null);
        await recent.OpenRecentCaseAsync(CasePath);
        Assert.Equal(picker.L["WrongDocumentType"], picker.StatusMessage);
        Assert.Equal(picker.StatusMessage, recent.StatusMessage);
        Assert.Empty(Store.Load());
        await File.WriteAllTextAsync(CasePath, "{unreadable");
        await picker.OpenCommand.ExecuteAsync(null);
        await recent.OpenRecentCaseAsync(CasePath);
        Assert.True(picker.IsError);
        Assert.Equal(picker.StatusMessage, recent.StatusMessage);
        Assert.Empty(Store.Load());
    }

    [Fact]
    public async Task MovedCaseCanBeRemovedFromTheListWithoutDeletingItsFile()
    {
        await WriteCaseAsync(CasePath, "Fictitious moved case");
        Store.RecordSaved(CasePath, "Fictitious moved case");
        var moved = Path.Combine(_folder.FullName, "moved" + DraftFile.Extension);
        File.Move(CasePath, moved);
        using var home = new HomeViewModel(Store, new ProfileStore(_folder.FullName), () => Task.CompletedTask);
        await Assert.Single(home.RecentCases).OpenCommand.ExecuteAsync(null);
        Assert.True(home.HasMissingFile);
        Assert.Equal(home.L.Format("RecentMissing", CasePath), home.Message);
        home.RemoveMissingCommand.Execute(null);
        Assert.Empty(Store.Load());
        Assert.True(File.Exists(moved));
        Assert.Equal("Fictitious moved case", (await DraftFile.LoadAsync(moved)).Inputs["claim.subject"]);
    }

    [Fact]
    public async Task DesktopAdapterExposesLocalPathsAndClearsCancelledOrFailedPickerMetadata()
    {
        await WriteCaseAsync(CasePath, "Fictitious desktop");
        var files = new AvaloniaUserFiles(() => null, new AppPaths(_folder.FullName));
        var opened = await files.OpenPathAsync(CasePath);
        Assert.NotNull(opened);
        await opened.Value.Stream.DisposeAsync();
        Assert.Equal(CasePath, files.LastOpenedPath);
        Assert.Null(await files.OpenAsync());
        Assert.Null(files.LastOpenedPath);
        await Assert.ThrowsAsync<FileNotFoundException>(() => files.OpenPathAsync(Path.Combine(_folder.FullName, "missing" + DraftFile.Extension)));
        Assert.Null(files.LastOpenedPath);
        Assert.Null(await files.SaveAsync("fictional", SaveKind.Draft));
        Assert.Null(files.LastSavedPath);
    }

    [Fact]
    public async Task SharedWebFileServiceStillSavesAndOpensWithoutARecentStore()
    {
        var files = new FakeFiles();
        using var model = Vm.Create(files);
        model.Field("claim.subject").Value = "Fictitious web";
        await model.SaveCommand.ExecuteAsync(null);
        using var opened = Vm.Create(files);
        await opened.OpenCommand.ExecuteAsync(null);
        Assert.Equal("Fictitious web", opened.Field("claim.subject").Value);
        Assert.False(opened.HasUnsavedChanges);
    }

    private static Task WriteCaseAsync(string path, string subject) => DraftFile.SaveAsync(new DraftDocument
    {
        DocumentTypeId = "requete-introductive",
        Inputs = new Dictionary<string, string> { ["claim.subject"] = subject },
    }, path);

    private sealed class LocalFiles : IUserFiles, IHomeRecentFiles
    {
        public string? OpenPath { get; set; }
        public string? SavePath { get; set; }
        public bool FailWrite { get; set; }
        public int PickerReads { get; private set; }
        public int PathReads { get; private set; }
        public List<SaveKind> SavedKinds { get; } = [];
        public string? LastOpenedPath { get; private set; }
        public string? LastSavedPath { get; private set; }

        public Task<(Stream Stream, string Name)?> OpenAsync()
        {
            PickerReads++;
            LastOpenedPath = null;
            if (OpenPath is null) return Task.FromResult<(Stream, string)?>(null);
            LastOpenedPath = OpenPath;
            return Task.FromResult<(Stream, string)?>((File.OpenRead(OpenPath), Path.GetFileName(OpenPath)));
        }
        public Task<(Stream Stream, string Name)?> OpenPathAsync(string path)
        {
            PathReads++;
            LastOpenedPath = path;
            return Task.FromResult<(Stream, string)?>((File.OpenRead(path), Path.GetFileName(path)));
        }
        public Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName, SaveKind kind)
        {
            LastSavedPath = null;
            if (SavePath is null) return Task.FromResult<(Stream, string)?>(null);
            SavedKinds.Add(kind);
            if (kind != SaveKind.Draft) return Task.FromResult<(Stream, string)?>((new MemoryStream(), suggestedName));
            LastSavedPath = SavePath;
            Stream stream = FailWrite ? new MemoryStream([], writable: false) : File.Create(SavePath);
            return Task.FromResult<(Stream, string)?>((stream, Path.GetFileName(SavePath)));
        }
        public Task CopyTextAsync(string text) => Task.CompletedTask;
    }

    public void Dispose() => _folder.Delete(true);
}
