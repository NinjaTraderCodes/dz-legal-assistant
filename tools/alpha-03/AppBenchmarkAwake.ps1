if(-not ('DzAppBenchmarkAwakeNative' -as [type])){
    Add-Type -TypeDefinition @"
using System.Runtime.InteropServices;
public static class DzAppBenchmarkAwakeNative {
    [DllImport("kernel32.dll", SetLastError=true)] public static extern uint SetThreadExecutionState(uint flags);
}
"@
}
function Enter-AppBenchmarkAwake {
    $previous=[DzAppBenchmarkAwakeNative]::SetThreadExecutionState([uint32]2147483649)
    if($previous -eq 0){throw 'Cannot prevent sleep; no timed run may start.'}
    $previous
}
function Exit-AppBenchmarkAwake {
    param([uint32]$Previous)
    if([DzAppBenchmarkAwakeNative]::SetThreadExecutionState($Previous) -eq 0){throw 'Could not restore the prior thread execution state.'}
}
