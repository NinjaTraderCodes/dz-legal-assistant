using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.App.Views;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public sealed class SetupTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-setup-");
    private readonly SetupFiles _files = new();
    private int _finished;
    private int _later;
    private SetupViewModel Create(bool edit = false, string? root = null, Action<bool>? theme = null) => new(
        new ProfileStore(root ?? _folder.FullName), new PreferencesStore(root ?? _folder.FullName),
        new ThemePreferences(root ?? _folder.FullName), new WelcomePreferences(root ?? _folder.FullName),
        new FakeFiles(), () => _finished++, () => _later++, onThemeApplied: theme,
        setupFiles: _files, isEditing: edit);

    [Fact]
    public void ThreeStepsHaveBoundedNextBackAndCorrectProgress()
    {
        var vm = Create();
        Assert.Equal(3, SetupViewModel.StepCount);
        vm.GoBackCommand.Execute(null);
        Assert.True(vm.ShowLawyerStep);
        vm.GoNextCommand.Execute(null);
        Assert.True(vm.ShowOfficeStep);
        Assert.True(vm.HasPrevious);
        vm.GoNextCommand.Execute(null);
        Assert.True(vm.ShowPrefsStep);
        Assert.True(vm.IsLastStep);
        Assert.False(vm.HasNext);
        vm.GoNextCommand.Execute(null);
        Assert.Equal(2, vm.Step);
        vm.GoBackCommand.Execute(null);
        Assert.Equal(1, vm.Step);
        Assert.Contains("2", vm.ProgressText, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptySetupCanFinishAndIsCleanWhenReloaded()
    {
        var vm = Create();
        Assert.False(vm.IsDirty);
        vm.FinishCommand.Execute(null);
        Assert.Equal(1, _finished);
        Assert.True(new WelcomePreferences(_folder.FullName).HasProfile);
        Assert.True(new WelcomePreferences(_folder.FullName).HasSetupSeen);
        Assert.False(Create().IsDirty);
    }

    [Theory]
    [InlineData(120, true)]
    [InlineData(121, false)]
    public void OverlongNameIsKeptWholeAndCannotBeSaved(int length, bool valid)
    {
        var vm = Create();
        vm.NameAr = new string('س', length);
        vm.NameFr = new string('a', length);
        Assert.Equal(length, vm.NameAr.Length);
        Assert.Equal(valid, vm.IsValid);
        vm.FinishCommand.Execute(null);
        Assert.Equal(valid ? 1 : 0, _finished);
        Assert.Equal(!valid, vm.HasNameError);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("fictitious@example.invalid", true)]
    [InlineData("not-an-email", false)]
    [InlineData("a@", false)]
    [InlineData("Name <a@example.invalid>", false)]
    public void OptionalEmailChecksShape(string email, bool valid)
    {
        var vm = Create();
        vm.Email = email;
        Assert.Equal(valid, vm.IsEmailValid);
        vm.SaveCommand.Execute(null);
        Assert.Equal(valid, File.Exists(Path.Combine(_folder.FullName, "profile.json")));
    }

    [Theory]
    [InlineData("male", false, "محامٍ لدى مجلس قضاء الجزائر")]
    [InlineData("female", false, "محامية لدى مجلس قضاء الجزائر")]
    [InlineData("male", true, "محامٍ لدى مجلس قضاء الجزائر، معتمد لدى المحكمة العليا ومجلس الدولة")]
    [InlineData("female", true, "محامية لدى مجلس قضاء الجزائر، معتمدة لدى المحكمة العليا ومجلس الدولة")]
    public void StandingFollowsInputsUntilManuallyEdited(string gender, bool accredited, string expected)
    {
        var vm = Create();
        vm.Council = "الجزائر";
        vm.Gender = gender;
        vm.SupremeCourt = accredited;
        Assert.Equal(expected, vm.Standing);
        Assert.Equal(vm.L[gender == "male" ? "ProfileSupremeCourtMale" : "ProfileSupremeCourtFemale"], vm.AccreditationLabel);
        vm.Standing = "صفة يختارها المحامي";
        vm.Council = "وهران";
        Assert.Equal("صفة يختارها المحامي", vm.Standing);
        vm.SaveCommand.Execute(null);
        Assert.True(new ProfileStore(_folder.FullName).Load().StandingEdited);
        var reloaded = Create();
        reloaded.Council = "قسنطينة";
        Assert.Equal("صفة يختارها المحامي", reloaded.Standing);
        reloaded.RestoreStandingCommand.Execute(null);
        Assert.Contains("قسنطينة", reloaded.Standing, StringComparison.Ordinal);
        reloaded.Council = string.Empty;
        Assert.Equal(string.Empty, reloaded.Standing);
    }

    [Fact]
    public void LoadsAllProfileFieldsAndPreferencesWithoutChangingSavedCases()
    {
        var profile = new LawyerProfile
        {
            NameAr = "محامية افتراضية", NameFr = "Avocate fictive", Gender = "female", Council = "وهران",
            Bar = "منظمة افتراضية", SupremeCourt = true, Standing = "صفة خاصة", StandingEdited = true,
            Address = "عنوان افتراضي\nسطر ثان", Phone = "0000000000", Email = "fictitious@example.invalid", Firm = "مكتب افتراضي",
            DefaultPlace = "وهران", DefaultCouncil = "الجزائر", DefaultCourt = "منظمة افتراضية", WatermarkByDefault = false,
            FileNaming = "parties-date",
        };
        var preferences = new ProfilePreferences { Language = "en", Theme = "light", Engine = "stub", AnalyseInBackground = false, SaveFolder = _folder.FullName };
        new ProfileStore(_folder.FullName).Save(profile);
        new PreferencesStore(_folder.FullName).Save(preferences);
        var vm = Create(true);
        Assert.Equal(profile.NameAr, vm.NameAr);
        Assert.Equal(profile.NameFr, vm.NameFr);
        Assert.Equal(profile.Address, vm.Address);
        Assert.Equal(profile.DefaultCourt, vm.DefaultCourt);
        Assert.False(vm.WatermarkByDefault);
        Assert.Equal("en", vm.L.Language);
        Assert.Equal("light", vm.Theme);
        Assert.False(vm.AnalyseInBackground);
        Assert.False(vm.IsDirty);
        vm.SaveCommand.Execute(null);
        Assert.Equal(profile, new ProfileStore(_folder.FullName).Load());
        Assert.Equal(preferences, new PreferencesStore(_folder.FullName).Load());
        Assert.Equal(0, _finished);
        Assert.Equal("Profile saved.", vm.Message);
    }

    [Fact]
    public void DirtyEditsCanBeCancelledDiscardedOrSavedBeforeLeaving()
    {
        var vm = Create();
        vm.NameAr = "اسم افتراضي";
        Assert.True(vm.IsDirty);
        vm.NameAr = string.Empty;
        Assert.False(vm.IsDirty);
        vm.NameAr = "اسم افتراضي";
        vm.LaterCommand.Execute(null);
        Assert.True(vm.IsConfirmingLeave);
        Assert.False(vm.CanEdit);
        Assert.Equal(0, _later);
        vm.CancelLeaveCommand.Execute(null);
        Assert.True(vm.IsDirty);
        vm.LaterCommand.Execute(null);
        vm.SaveAndLeaveCommand.Execute(null);
        Assert.Equal(1, _later);
        Assert.False(vm.IsDirty);
        Assert.Equal("اسم افتراضي", new ProfileStore(_folder.FullName).Load().NameAr);
        vm.NameAr = "تعديل افتراضي";
        var closed = false;
        vm.RequestLeave(() => closed = true);
        vm.DiscardAndLeaveCommand.Execute(null);
        Assert.True(closed);
        Assert.Equal("اسم افتراضي", vm.NameAr);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void LaterNeverCreatesAFakeProfile()
    {
        Create().LaterCommand.Execute(null);
        Assert.Equal(1, _later);
        var preferences = new WelcomePreferences(_folder.FullName);
        Assert.False(preferences.HasProfile);
        Assert.True(preferences.HasSetupSeen);
    }

    [Fact]
    public void WriteFailuresKeepEditsAndDoNotNavigate()
    {
        var blocked = Path.Combine(_folder.FullName, "a-file");
        File.WriteAllText(blocked, "fixture");
        var vm = Create(root: blocked);
        vm.NameAr = "اسم افتراضي";
        vm.FinishCommand.Execute(null);
        Assert.Equal(0, _finished);
        Assert.True(vm.IsDirty);
        Assert.Equal("ProfileSaveFailed", vm.MessageKey);
        Assert.True(vm.IsError);
        var laterVm = Create(root: blocked);
        laterVm.LaterCommand.Execute(null);
        Assert.Equal("SetupMarkerFailed", laterVm.MessageKey);
        Assert.Equal(0, _later);
        Assert.True(laterVm.CanContinueAnyway);
        laterVm.ContinueAnywayCommand.Execute(null);
        Assert.Equal(1, _later);
    }

    [Fact]
    public async Task LanguageThemeAndFolderApplyFromTheirRealBindings()
    {
        bool? appliedDark = null;
        var vm = Create(theme: dark => appliedDark = dark);
        vm.Language = "en";
        Assert.Equal("en", vm.L.Language);
        Assert.False(vm.IsRightToLeft);
        Assert.Equal("Full name (Arabic)", vm.L["ProfileNameAr"]);
        vm.Theme = "light";
        Assert.False(appliedDark);
        Assert.False(new ThemePreferences(_folder.FullName).LoadDarkTheme());
        _files.Folder = _folder.FullName;
        await vm.ChooseSaveFolderCommand.ExecuteAsync(null);
        Assert.Equal(_folder.FullName, vm.SaveFolder);
        Assert.Equal("en", new WelcomePreferences(_folder.FullName).LoadLanguage());
        vm.RequestLeave(() => { });
        vm.DiscardAndLeaveCommand.Execute(null);
        Assert.True(vm.IsRightToLeft);
        Assert.True(appliedDark);
    }

    [Fact]
    public void AlreadySavedTitleBarThemeChangesDoNotDiscardOtherUnsavedProfileEdits()
    {
        var vm = Create(true);
        vm.ObserveTheme(false);
        Assert.False(vm.IsDirty);
        vm.NameAr = "اسم افتراضي";
        vm.ObserveTheme(true);
        Assert.True(vm.IsDirty);
        vm.RequestLeave(() => { });
        vm.DiscardAndLeaveCommand.Execute(null);
        Assert.Equal(string.Empty, vm.NameAr);
        Assert.Equal("dark", vm.Theme);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task LogoIsValidatedCopiedAndRemovedWithoutDeletingSavedImage()
    {
        var vm = Create();
        _files.Image = new SetupImage("logo.png", Png);
        await vm.ChooseLogoCommand.ExecuteAsync(null);
        Assert.True(vm.HasLogo);
        var copiedPath = Path.Combine(_folder.FullName, vm.Logo);
        Assert.Equal(Png, File.ReadAllBytes(copiedPath));
        vm.SaveCommand.Execute(null);
        Assert.True(Create().HasLogo);
        _files.Image = new SetupImage("large.png", new byte[OnboardingProfileAssets.MaximumBytes + 1]);
        await vm.ChooseLogoCommand.ExecuteAsync(null);
        Assert.Equal("ImageTooLarge", vm.MessageKey);
        _files.Image = new SetupImage("broken.png", [1, 2, 3]);
        await vm.ChooseLogoCommand.ExecuteAsync(null);
        Assert.Equal("ImageInvalid", vm.MessageKey);
        Assert.True(vm.HasLogo);
        vm.RemoveLogoCommand.Execute(null);
        Assert.False(vm.HasLogo);
        Assert.True(File.Exists(copiedPath));
        Assert.Null(new OnboardingProfileAssets(_folder.FullName).Read("../outside.png"));
    }

    [Fact]
    public async Task ImportRequiresConfirmationAndInvalidImportChangesNothing()
    {
        var importPath = Path.Combine(_folder.FullName, "import.dzprofile.json");
        ProfileStore.Export(importPath, new LawyerProfile { NameAr = "اسم مستورد افتراضي" },
            new ProfilePreferences { Language = "en" }, new ExportedLogo("logo.png", Convert.ToBase64String(Png)));
        _files.ImportPath = importPath;
        var vm = Create();
        vm.NameAr = "اسم افتراضي";
        await vm.ImportCommand.ExecuteAsync(null);
        Assert.True(vm.IsConfirmingImport);
        Assert.Equal("اسم افتراضي", vm.NameAr);
        vm.CancelImportCommand.Execute(null);
        Assert.Equal("اسم افتراضي", vm.NameAr);
        await vm.ImportCommand.ExecuteAsync(null);
        vm.ConfirmImportCommand.Execute(null);
        Assert.Equal("اسم مستورد افتراضي", vm.NameAr);
        Assert.True(vm.HasLogo);
        Assert.True(vm.IsDirty);
        Assert.False(File.Exists(Path.Combine(_folder.FullName, "profile.json")));
        vm.SaveCommand.Execute(null);
        _files.ExportPath = Path.Combine(_folder.FullName, "export.dzprofile.json");
        await vm.ExportCommand.ExecuteAsync(null);
        Assert.NotNull(ProfileStore.Import(_files.ExportPath)?.Logo);
        File.WriteAllText(importPath, "{broken");
        await vm.ImportCommand.ExecuteAsync(null);
        Assert.Equal("ProfileImportInvalid", vm.MessageKey);
        Assert.Equal("اسم مستورد افتراضي", vm.NameAr);
        Assert.False(vm.IsConfirmingImport);
    }

    [AvaloniaFact]
    public void RealWelcomeFactoryLoadsAllThreeViewsAndSavedLaunchSkipsSetup()
    {
        var preferences = new WelcomePreferences(_folder.FullName);
        var window = new OnboardingWindow(preferences, () => new Window());
        Assert.IsType<WelcomeView>(window.Content);
        window.OpenSetupStepOne();
        var host = Assert.IsType<Grid>(window.Content);
        Assert.IsType<ThemeToggleButton>(Assert.IsType<Border>(host.Children[0]).Child);
        var setup = Assert.Single(host.Children.OfType<SetupView>());
        Assert.IsType<SetupStepLawyerView>(setup.FindControl<ContentControl>("SetupStepHost")!.Content);
        window.Setup.GoNextCommand.Execute(null);
        Assert.IsType<SetupStepOfficeView>(setup.FindControl<ContentControl>("SetupStepHost")!.Content);
        window.Setup.GoNextCommand.Execute(null);
        Assert.IsType<SetupStepPrefsView>(setup.FindControl<ContentControl>("SetupStepHost")!.Content);
        window.Setup.SaveCommand.Execute(null);
        window.Close();
        var destination = new Border();
        var relaunch = new OnboardingWindow(preferences, () => new Window(), startScreen: () => destination);
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(relaunch.Content).DataContext).StartCommand.Execute(null);
        Assert.Same(destination, relaunch.Content);
        relaunch.Close();
        var editor = new ProfileSettingsWindow(_folder.FullName);
        Assert.True(editor.Setup.IsEditing);
        Assert.Single(Assert.IsType<Grid>(editor.Content).Children.OfType<SetupView>());
        editor.Close();
    }

    [AvaloniaFact]
    public void SetupScreensRenderWithReadableLabelsAndFixedNavigationInTheFullMatrix()
    {
        foreach (var scale in new[] { 1d, 1.25 })
        foreach (var size in new[] { (1366, 768), (1920, 1080) })
        foreach (var language in new[] { "ar", "en" })
        foreach (var dark in new[] { false, true })
        {
            var vm = Create();
            vm.NameAr = "محامية افتراضية";
            vm.NameFr = "Avocate fictive";
            vm.Gender = "female";
            vm.Council = "الجزائر";
            vm.Language = language;
            var view = new SetupView(false) { DataContext = vm };
            var window = new Window { Width = size.Item1 / scale, Height = size.Item2 / scale,
                RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light, Content = view };
            window.Show();
            window.SetRenderScaling(scale);
            Assert.True(view.TryFindResource("WelcomeOverlayBrush", window.ActualThemeVariant, out var overlay));
            Assert.IsType<LinearGradientBrush>(overlay);
            Assert.True(view.TryFindResource("SetupSurfaceBrush", window.ActualThemeVariant, out var surface));
            Assert.IsType<SolidColorBrush>(surface);
            for (var step = 0; step < 3; step++)
            {
                vm.Step = step;
                Layout(window);
                Assert.Equal(language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, view.FlowDirection);
                var host = view.FindControl<ContentControl>("SetupStepHost")!;
                Assert.NotNull(host.Content);
                Assert.True(host.Bounds.Height > 100);
                foreach (var label in view.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible))
                {
                    Assert.DoesNotContain("[Profile", label.Text ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain("[Setup", label.Text ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain("{0}", label.Text ?? string.Empty, StringComparison.Ordinal);
                }
                foreach (var name in new[] { "SetupBack", "SetupLater", "SetupNext", "SetupFinish" })
                {
                    var button = view.FindControl<Button>(name)!;
                    if (!button.IsEffectivelyVisible) continue;
                    var origin = button.TranslatePoint(default, window)!.Value;
                    Assert.True(origin.Y >= 0 && origin.Y + button.Bounds.Height <= window.Bounds.Height + 1);
                    Assert.True(origin.X >= 0 && origin.X + button.Bounds.Width <= window.Bounds.Width + 1);
                }
                if (scale == 1 && size.Item1 == 1366)
                    Capture(window, $"setup-{step + 1}-{language}-{(dark ? "dark" : "light")}-{size.Item1}x{size.Item2}");
                if (step == 1)
                {
                    var scroll = view.FindControl<ScrollViewer>("StepScroll")!;
                    scroll.Offset = new Vector(0, scroll.Extent.Height);
                    Layout(window);
                    if (scale == 1 && size.Item1 == 1366)
                        Capture(window, $"setup-office-lower-{language}-{(dark ? "dark" : "light")}-{size.Item1}x{size.Item2}");
                }
            }
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LogoDecoderRejectsSignatureOnlyFilesAndAcceptsRealPng()
    {
        var files = new SetupFileDialogs(() => null);
        Assert.True(files.IsReadableImage(Png));
        Assert.False(files.IsReadableImage([137, 80, 78, 71, 13, 10, 26, 10]));
    }

    [AvaloniaFact]
    public async Task ProfileEditorShowsSavedValidationImportAndUnsavedStatesInBothLanguagesAndThemes()
    {
        foreach (var language in new[] { "ar", "en" })
        foreach (var dark in new[] { false, true })
        foreach (var width in new[] { 1366, 1920 })
        {
            var vm = Create(true);
            vm.Language = language;
            vm.Theme = dark ? "dark" : "light";
            vm.NameAr = "محامية افتراضية";
            vm.NameFr = "Avocate fictive";
            vm.Council = "الجزائر";
            vm.Gender = "female";
            vm.SaveCommand.Execute(null);
            var view = new SetupView(false) { DataContext = vm };
            var window = new Window { Width = width, Height = width == 1366 ? 768 : 1080,
                Content = view, RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light };
            window.Show();
            Layout(window);
            var suffix = $"{language}-{(dark ? "dark" : "light")}-{width}";
            Assert.False(vm.IsDirty);
            vm.SaveCommand.Execute(null);
            Layout(window);
            Assert.Equal("ProfileSaved", vm.MessageKey);
            Capture(window, "profile-saved-" + suffix);
            var importPath = Path.Combine(_folder.FullName, "screenshot.dzprofile.json");
            ProfileStore.Export(importPath, new LawyerProfile { NameAr = "محامٍ مستورد افتراضي", Council = "وهران" },
                new ProfilePreferences { Language = language, Theme = vm.Theme }, null);
            _files.ImportPath = importPath;
            await vm.ImportCommand.ExecuteAsync(null);
            Layout(window);
            Assert.True(vm.IsConfirmingImport);
            Capture(window, "profile-import-confirm-" + suffix);
            vm.CancelImportCommand.Execute(null);
            vm.Email = "invalid";
            vm.SaveCommand.Execute(null);
            Layout(window);
            Assert.True(vm.HasEmailError);
            Assert.Equal(1, vm.Step);
            Capture(window, "profile-validation-" + suffix);
            vm.RequestLeave(() => { });
            Layout(window);
            Assert.True(vm.IsConfirmingLeave);
            Assert.False(vm.CanEdit);
            Capture(window, "profile-unsaved-" + suffix);
            vm.CancelLeaveCommand.Execute(null);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BindingSelectionsKeepTheirValuesAndLanguageSwitchRelocalizesInPlace()
    {
        var vm = Create();
        vm.Gender = "female";
        vm.Language = "en";
        vm.Theme = "light";
        vm.FileNaming = "parties-date";
        var view = new SetupView(false) { DataContext = vm };
        var window = new Window { Width = 1366, Height = 768, Content = view };
        window.Show();
        for (var step = 0; step < 3; step++)
        {
            vm.Step = step;
            Layout(window);
            Assert.Equal("female", vm.Gender);
            Assert.Equal("en", vm.Language);
            Assert.Equal("light", vm.Theme);
            Assert.Equal("parties-date", vm.FileNaming);
        }
        var prefs = Assert.IsType<SetupStepPrefsView>(view.FindControl<ContentControl>("SetupStepHost")!.Content);
        prefs.FindControl<ComboBox>("ProfileLanguage")!.SelectedIndex = 0;
        Layout(window);
        Assert.Equal("ar", vm.Language);
        Assert.True(vm.IsRightToLeft);
        Assert.Equal("female", vm.Gender);
        Assert.Equal(vm.L["PrefsThemeLight"], vm.ThemeOptions[0].Label);
        window.Close();
    }

    [Fact]
    public void EverySetupBindingHasTemporaryArabicAndEnglishWording()
    {
        foreach (var (key, text) in SetupLocalization.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(text.Arabic), key);
            Assert.False(string.IsNullOrWhiteSpace(text.English), key);
        }
        var vm = Create();
        foreach (var choice in vm.GenderOptions.Concat(vm.LanguageOptions).Concat(vm.ThemeOptions).Concat(vm.EngineOptions).Concat(vm.FileNamingOptions))
            Assert.False(string.IsNullOrWhiteSpace(choice.Label));
    }

    private static void Layout(Window window)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.CaptureRenderedFrame()?.Dispose();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Capture(Window window, string name)
    {
        if (Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        using var bitmap = window.CaptureRenderedFrame();
        Assert.NotNull(bitmap);
        bitmap.Save(Path.Combine(directory, name + ".png"), PngBitmapEncoderOptions.Default);
    }

    private static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAI0lEQVR4nGPcMi2FgRTARJJqhlENxAEmItXBwagGYgDJoQQAVi8BzpMzEGYAAAAASUVORK5CYII=");

    private sealed class SetupFiles : ISetupFiles
    {
        public SetupImage? Image { get; set; }
        public string? Folder { get; set; }
        public string? ImportPath { get; set; }
        public string? ExportPath { get; set; }
        public Task<SetupImage?> ChooseImageAsync() => Task.FromResult(Image);
        public Task<string?> ChooseFolderAsync(string? currentFolder) => Task.FromResult(Folder);
        public Task<string?> ChooseImportAsync() => Task.FromResult(ImportPath);
        public Task<string?> ChooseExportAsync() => Task.FromResult(ExportPath);
        public bool IsReadableImage(byte[] bytes) => bytes.SequenceEqual(Png);
    }

    public void Dispose() => _folder.Delete(recursive: true);
}
