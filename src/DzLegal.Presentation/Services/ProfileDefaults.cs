using DzLegal.Storage;

namespace DzLegal.Presentation.Services;

/// <summary>Profile values read from the per-user profile store.</summary>
public sealed class ProfileDefaults(string root) : IProfileDefaults
{
    private readonly ProfileStore _profileStore = new(root);

    public IReadOnlyDictionary<string, string> Values
    {
        get
        {
            var profile = _profileStore.Load();
            var values = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(profile.NameAr)) values["lawyer.name"] = profile.NameAr;
            if (!string.IsNullOrWhiteSpace(profile.Gender)) values["lawyer.gender"] = profile.Gender;
            if (!string.IsNullOrWhiteSpace(profile.Standing)) values["lawyer.bar"] = profile.Standing;
            if (!string.IsNullOrWhiteSpace(profile.Address)) values["lawyer.address"] = profile.Address;
            if (!string.IsNullOrWhiteSpace(profile.DefaultPlace)) values["document.place"] = profile.DefaultPlace;
            if (!string.IsNullOrWhiteSpace(profile.DefaultCouncil)) values["court.council"] = profile.DefaultCouncil;
            if (!string.IsNullOrWhiteSpace(profile.DefaultCourt)) values["court.name"] = profile.DefaultCourt;
            return values;
        }
    }

    public bool WatermarkByDefault => _profileStore.Load().WatermarkByDefault;
}
