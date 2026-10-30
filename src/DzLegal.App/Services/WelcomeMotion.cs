using System.Runtime.InteropServices;

namespace DzLegal.App.Services;

internal static partial class WelcomeMotion
{
    internal static bool IsEnabled()
    {
        // Fail closed when the platform cannot tell us the user's motion preference.
        return OperatingSystem.IsWindows()
            && SystemParametersInfo(0x1042, 0, out var enabled, 0) != 0
            && enabled != 0;
    }

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static partial int SystemParametersInfo(uint action, uint parameter, out int value, uint flags);
}
