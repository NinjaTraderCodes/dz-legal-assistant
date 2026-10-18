namespace DzLegal.Presentation.Services;

/// <summary>
/// Whether the computer runs on battery or in power-saver mode, where the local model is much slower. The lawyer is
/// told, so a slow draft is not taken for a fault.
/// </summary>
public interface IPowerStatus
{
    bool IsSavingPower();
}

/// <summary>Where the power state is unknown (tests, other systems): never warns.</summary>
public sealed class NoPowerStatus : IPowerStatus
{
    public bool IsSavingPower() => false;
}
