param(
    [Parameter(Mandatory=$true)][string]$Project,
    [string]$OutputDirectory = 'artifacts/packages'
)
$ErrorActionPreference = 'Stop'
$projectPath = (Resolve-Path -LiteralPath $Project).Path
$projectDirectory = Split-Path -Parent $projectPath
$manifestPath = Join-Path $projectDirectory 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.id -notmatch '^[a-z0-9]+([.-][a-z0-9]+)*$' -or $manifest.version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw 'Invalid manifest identity.' }
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($outputPath) | Out-Null
$staging = Join-Path $projectDirectory ('obj/package-' + [Guid]::NewGuid().ToString('N'))
$publish = Join-Path $staging 'publish'
$content = Join-Path $staging 'content'
try {
    dotnet publish $projectPath --configuration Release --output $publish --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Extension build failed.' }
    [IO.Directory]::CreateDirectory((Join-Path $content 'Services')) | Out-Null
    Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $content 'manifest.json')
    Get-ChildItem -LiteralPath $publish -File | Where-Object { $_.Name -ne 'HIT.Sdk.dll' -and $_.Name -ne 'HIT.Sdk.pdb' -and $_.Extension -in '.dll','.json' } | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $content 'Services')
    }
    Get-ChildItem -LiteralPath $publish -Directory | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $content 'Services') -Recurse
    }
    foreach ($relative in @('Layout/css','Assets','Dependencies')) {
        $source = Join-Path $projectDirectory $relative
        if (Test-Path -LiteralPath $source) {
            $destination = Join-Path $content $relative
            [IO.Directory]::CreateDirectory((Split-Path -Parent $destination)) | Out-Null
            Copy-Item -LiteralPath $source -Destination $destination -Recurse
        }
    }
    $package = Join-Path $outputPath ($manifest.id + '-' + $manifest.version + '.hitmenu')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    Add-Type -AssemblyName System.IO.Compression
    $temporary = Join-Path $staging 'result.zip'
    $archive = [IO.Compression.ZipFile]::Open($temporary, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem -LiteralPath $content -File -Recurse | ForEach-Object {
            $relative = $_.FullName.Substring($content.Length).TrimStart([char]'\').Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $relative) | Out-Null
        }
    }
    finally { $archive.Dispose() }
    Move-Item -LiteralPath $temporary -Destination $package -Force
    Write-Output $package
}
finally {
    $verified = [IO.Path]::GetFullPath($staging)
    $allowed = [IO.Path]::GetFullPath((Join-Path $projectDirectory 'obj')) + [IO.Path]::DirectorySeparatorChar
    if ($verified.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $verified)) { Remove-Item -LiteralPath $verified -Recurse -Force }
}
