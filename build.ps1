$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
New-Item -ItemType Directory -Force dist,tests,assets | Out-Null
Add-Type -AssemblyName System.Drawing
# Vector artwork rendered at each Windows icon size, preserving sharp small icons.
$frames = @()
foreach ($size in @(16,24,32,48,64,128,256)) {
 $bmp = New-Object Drawing.Bitmap($size,$size)
 $g = [Drawing.Graphics]::FromImage($bmp)
 $g.SmoothingMode = 'AntiAlias'
 $g.ScaleTransform($size/256.0,$size/256.0)
 $navy = [Drawing.ColorTranslator]::FromHtml('#122438')
 $mint = [Drawing.ColorTranslator]::FromHtml('#40d9c0')
 $amber = [Drawing.ColorTranslator]::FromHtml('#ffbc66')
 $bg = New-Object Drawing.SolidBrush($navy)
 $g.FillRectangle($bg,8,8,240,240)
 $pen = New-Object Drawing.Pen($mint,19)
 $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
 $g.DrawArc($pen,58,51,140,140,302,296)
 $g.DrawLine($pen,128,42,128,112)
 $ab = New-Object Drawing.SolidBrush($amber)
 $g.FillEllipse($ab,133,131,102,102)
 $np = New-Object Drawing.Pen($navy,9)
 $g.DrawEllipse($np,133,131,102,102)
 $np.StartCap = 'Round'; $np.EndCap = 'Round'
 $g.DrawLine($np,184,152,184,183); $g.DrawLine($np,184,183,205,196)
 $stream = New-Object IO.MemoryStream
 $bmp.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
 $frames += ,@($size,$stream.ToArray())
 if ($size -eq 256) { $bmp.Save((Join-Path $PSScriptRoot 'assets\icon.png'),[Drawing.Imaging.ImageFormat]::Png) }
 $stream.Dispose(); $g.Dispose(); $bmp.Dispose(); $pen.Dispose(); $np.Dispose(); $bg.Dispose(); $ab.Dispose()
}
$output = [IO.File]::Create((Join-Path $PSScriptRoot 'assets\app.ico'))
$writer = New-Object IO.BinaryWriter($output)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
$offset = 6 + 16*$frames.Count
foreach ($frame in $frames) {
 $dimension = $frame[0] % 256
 $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([uint16]0)
 $writer.Write([uint16]1); $writer.Write([uint16]32)
 $writer.Write([uint32]$frame[1].Length); $writer.Write([uint32]$offset)
 $offset += $frame[1].Length
}
foreach ($frame in $frames) { $writer.Write([byte[]]$frame[1]) }
$writer.Dispose()
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$refs = @('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Xml.Linq.dll','/r:Microsoft.CSharp.dll')
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /out:dist\PlannedShutdown-1.3.exe /win32manifest:src\app.manifest /win32icon:assets\app.ico /resource:assets\app.ico,AppIcon /resource:assets\icon.png,AppArtwork @refs src\*.cs
if ($LASTEXITCODE -ne 0) { throw 'Compilazione fallita' }
& $compiler /nologo /target:exe /out:tests\Checks.exe /main:PlannedShutdown.Checks /resource:assets\app.ico,AppIcon /resource:assets\icon.png,AppArtwork @refs src\*.cs tests\Checks.cs
if ($LASTEXITCODE -ne 0) { throw 'Compilazione test fallita' }
& .\tests\Checks.exe
if ($LASTEXITCODE -ne 0) { throw 'Test falliti' }
