[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $root

dotnet publish .\PortBan.csproj -c Release -r win-x64 --self-contained true "-p:Version=$Version" "-p:IncludeNativeLibrariesForSelfExtract=true"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish が失敗しました。"
}

$candidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)
$iscc = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) {
    $command = Get-Command iscc -ErrorAction SilentlyContinue
    if ($command) {
        $iscc = $command.Source
    }
}
if (-not $iscc) {
    throw "Inno Setup 6 が見つかりません。https://jrsoftware.org/isdl.php から入れてください。"
}

$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
& $iscc "/DAppVersion=$Version" (Join-Path $root "installer\portban.iss")
if ($LASTEXITCODE -ne 0) {
    throw "インストーラーの作成に失敗しました。"
}

$published = Join-Path $root "bin\Release\net8.0\win-x64\publish\PortBan.exe"
$portable = Join-Path $dist "PortBan-$Version.exe"
Copy-Item $published $portable -Force

$files = @(
    (Join-Path $dist "PortBan-Setup-$Version.exe"),
    $portable
)
$sums = foreach ($file in $files) {
    $hash = Get-FileHash -Path $file -Algorithm SHA256
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), (Split-Path -Leaf $file)
}
Set-Content -Path (Join-Path $dist "SHA256SUMS.txt") -Value $sums -Encoding ascii
