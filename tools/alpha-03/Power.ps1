if (-not ('DzAlphaPowerNative' -as [type])) {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class DzAlphaPowerNative {
    [StructLayout(LayoutKind.Sequential)] public struct Status {
        public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
        public uint BatteryLifeTime, BatteryFullLifeTime;
    }
    [DllImport("kernel32.dll")] public static extern bool GetSystemPowerStatus(out Status status);
    [DllImport("powrprof.dll")] public static extern uint PowerGetActualOverlayScheme(out Guid scheme);
    [DllImport("powrprof.dll")] public static extern uint PowerGetEffectiveOverlayScheme(out Guid scheme);
    [DllImport("powrprof.dll")] public static extern uint PowerGetUserConfiguredACPowerMode(out Guid scheme);
}
"@
}

function Get-AlphaPower {
    $status = New-Object DzAlphaPowerNative+Status
    if (-not [DzAlphaPowerNative]::GetSystemPowerStatus([ref]$status)) { throw 'Windows power query failed.' }
    $actual = [Guid]::Empty
    $effective = [Guid]::Empty
    $actualResult = [DzAlphaPowerNative]::PowerGetActualOverlayScheme([ref]$actual)
    $effectiveResult = [DzAlphaPowerNative]::PowerGetEffectiveOverlayScheme([ref]$effective)
    $configured = [Guid]::Empty
    $configuredResult = [DzAlphaPowerNative]::PowerGetUserConfiguredACPowerMode([ref]$configured)
    [pscustomobject]@{acLineStatus=$status.ACLineStatus;batteryPercent=$status.BatteryLifePercent;energySaver=$status.SystemStatusFlag;actualOverlayResult=$actualResult;actualOverlayGuid=$actual.ToString();effectiveOverlayResult=$effectiveResult;effectiveOverlayGuid=$effective.ToString();configuredAcResult=$configuredResult;configuredAcGuid=$configured.ToString();requiredMode='Best performance'}
}

function Assert-AlphaPower {
    $power = Get-AlphaPower
    $best = 'ded574b5-45a0-4f42-8737-46345c09c238'
    if ($power.acLineStatus -ne 1 -or $power.actualOverlayResult -ne 0 -or $power.effectiveOverlayResult -ne 0 -or $power.configuredAcResult -ne 0 -or $power.actualOverlayGuid -ne $best -or $power.effectiveOverlayGuid -ne $best -or $power.configuredAcGuid -ne $best) {
        throw ('Mains power and Best performance are required: ' + ($power | ConvertTo-Json -Compress))
    }
    $power
}
