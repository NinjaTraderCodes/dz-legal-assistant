using DzLegal.Storage;

namespace DzLegal.Storage.Tests;

public sealed class ProfileStorageTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("dzlegal-profile-");

    [Fact]
    public void MissingOrBrokenProfileFallsBackToDefaults()
    {
        var store = new ProfileStore(_dir.FullName);

        Assert.Equal(LawyerProfile.Default, store.Load());

        File.WriteAllText(Path.Combine(_dir.FullName, "profile.json"), "{ broken");
        Assert.Equal(LawyerProfile.Default, store.Load());
    }

    [Fact]
    public void ProfileRoundTripsAndKeepsArabicReadable()
    {
        var store = new ProfileStore(_dir.FullName);
        var profile = new LawyerProfile
        {
            NameAr = "بن علي",
            NameFr = "Ben Ali",
            Gender = "male",
            Council = "الجزائر",
            Bar = "منظمة المحامين",
            Standing = "محامٍ لدى مجلس قضاء الجزائر",
            Address = "شارع قسنطينة",
            DefaultPlace = "الجزائر",
            DefaultCouncil = "الجزائر",
            DefaultCourt = "المحكمة العليا",
        };

        store.Save(profile);
        var loaded = store.Load();

        Assert.Equal("بن علي", loaded.NameAr);
        Assert.Equal("Ben Ali", loaded.NameFr);
        Assert.Equal("male", loaded.Gender);
        Assert.Equal("الجزائر", loaded.Council);
        Assert.Equal("منظمة المحامين", loaded.Bar);
        Assert.Equal("محامٍ لدى مجلس قضاء الجزائر", loaded.Standing);
        Assert.Equal("شارع قسنطينة", loaded.Address);
        Assert.Equal("الجزائر", loaded.DefaultPlace);
        Assert.Equal("الجزائر", loaded.DefaultCouncil);
        Assert.Equal("المحكمة العليا", loaded.DefaultCourt);
        Assert.True(loaded.WatermarkByDefault);
        Assert.Equal("current", loaded.FileNaming);
    }

    [Fact]
    public void ProfileSaveIsAtomic()
    {
        var store = new ProfileStore(_dir.FullName);
        var path = store.ProfilePath;
        File.WriteAllText(path, "older");
        store.Save(new LawyerProfile());

        Assert.False(File.Exists(path + ".tmp"));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void SuggestedStanding_IsEmptyWithoutCouncilOrFormOfAddress()
    {
        Assert.Equal(string.Empty, LawyerProfile.SuggestedStanding("", false, "male"));
        Assert.Equal(string.Empty, LawyerProfile.SuggestedStanding("الجزائر", false, null));
    }

    [Fact]
    public void SuggestedStanding_UsesTheBriefText()
    {
        Assert.Equal("محامٍ لدى مجلس قضاء الجزائر", LawyerProfile.SuggestedStanding("الجزائر", accredited: false, "male"));
        Assert.Equal("محامية لدى مجلس قضاء الجزائر", LawyerProfile.SuggestedStanding("الجزائر", false, "female"));
        Assert.Equal("محامٍ لدى مجلس قضاء الجزائر، معتمد لدى المحكمة العليا ومجلس الدولة", LawyerProfile.SuggestedStanding("الجزائر", accredited: true, "male"));
        Assert.Equal("محامية لدى مجلس قضاء الجزائر، معتمدة لدى المحكمة العليا ومجلس الدولة", LawyerProfile.SuggestedStanding("الجزائر", accredited: true, "female"));
    }

    [Fact]
    public void ExportedProfileRoundTripsWithLogo()
    {
        var store = new ProfileStore(_dir.FullName);
        var logo = new ExportedLogo("logo.png", Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47]));
        var exported = new ExportedProfile(new LawyerProfile(), new ProfilePreferences(), logo);

        var path = ProfileStore.Export(Path.Combine(_dir.FullName, "profile.dzprofile.json"), exported.Profile, exported.Preferences, logo);
        var imported = ProfileStore.Import(path);

        Assert.NotNull(imported);
        Assert.Equal(exported.Profile, imported!.Profile);
        Assert.Equal(exported.Preferences, imported.Preferences);
        Assert.Equal(logo, imported.Logo);
    }

    [Fact]
    public void BrokenExportImportReturnsNull()
    {
        var store = new ProfileStore(_dir.FullName);
        var path = Path.Combine(_dir.FullName, "broken.dzprofile.json");
        File.WriteAllText(path, "{ broken");

        Assert.Null(ProfileStore.Import(path));
    }

    [Fact]
    public void PreferencesLoadDefaultToTheShippedBehaviour()
    {
        var store = new PreferencesStore(_dir.FullName);

        var loaded = store.Load();

        Assert.Equal("ar", loaded.Language);
        Assert.Equal("dark", loaded.Theme);
        Assert.Equal("ollama", loaded.Engine);
        Assert.True(loaded.AnalyseInBackground);
        Assert.Null(loaded.SaveFolder);
    }

    public void Dispose() => _dir.Delete(recursive: true);
}
