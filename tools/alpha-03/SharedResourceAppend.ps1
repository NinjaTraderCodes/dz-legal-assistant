function Add-SharedResourceLine {
    param([string]$Path,[string]$Line)
    $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Append,[IO.FileAccess]::Write,[IO.FileShare]::ReadWrite)
    try{
        $writer=[IO.StreamWriter]::new($stream,[Text.UTF8Encoding]::new($false),1024,$true)
        try{$writer.WriteLine($Line)}finally{$writer.Dispose()}
    } finally{$stream.Dispose()}
}
