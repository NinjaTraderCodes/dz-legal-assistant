param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$imageRoot=(Resolve-Path -LiteralPath $Folder).Path
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ($imageRoot -eq $repository -or $imageRoot.StartsWith($repository+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Screenshots must stay outside the repository.' }
$review=Join-Path $imageRoot 'review'
New-Item -ItemType Directory -Path $review -Force | Out-Null
$stages=Get-ChildItem -LiteralPath $imageRoot -Filter '*.png' | ForEach-Object {$_.BaseName.Split('-')[0]} | Sort-Object -Unique
$font=[Drawing.Font]::new('Segoe UI',11)
try {
    foreach($stage in $stages) {
        $sheet=[Drawing.Bitmap]::new(1520,2280)
        $graphics=[Drawing.Graphics]::FromImage($sheet)
        try {
            $graphics.Clear([Drawing.Color]::LightGray)
            $index=0
            foreach($size in @(1366,1920)) {
                foreach($language in @('ar','en')) {
                    foreach($theme in @('light','dark')) {
                        $height=if($size -eq 1366){768}else{1080}
                        $name="$stage-$language-$theme-${size}x${height}.png"
                        $source=[Drawing.Image]::FromFile((Join-Path $imageRoot $name))
                        try {
                            # MainWindow's two fixed side columns leave this centre card in either direction.
                            $left=if($language -eq 'ar'){350}else{410}
                            $width=$size-760
                            $scale=[Math]::Min(1.0,750.0/$width)
                            $column=$index%2
                            $row=[Math]::Floor($index/2)
                            $graphics.DrawString($name,$font,[Drawing.Brushes]::Black,($column*760+5),($row*570+4))
                            $target=[Drawing.Rectangle]::new([int]($column*760+5),[int]($row*570+30),[int]($width*$scale),[int](535*$scale))
                            $graphics.DrawImage($source,$target,[int]$left,75,[int]$width,535,[Drawing.GraphicsUnit]::Pixel)
                        } finally { $source.Dispose() }
                        $index++
                    }
                }
            }
            $sheet.Save((Join-Path $review ($stage+'.png')),[Drawing.Imaging.ImageFormat]::Png)
        } finally { $graphics.Dispose(); $sheet.Dispose() }
    }
} finally { $font.Dispose() }
Write-Output ('Review sheets prepared: '+$stages.Count)