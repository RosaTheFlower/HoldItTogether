param(
    [string]$ReleaseDirectory,
    [string]$Compiler,
    [switch]$TestBuild
)
$ErrorActionPreference='Stop'
$workspace=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location -LiteralPath $workspace
try {
    if (!$ReleaseDirectory) {
        & (Join-Path $PSScriptRoot 'Build-Release.ps1') -RunTests
        $ReleaseDirectory=(Get-ChildItem -LiteralPath 'artifacts/releases' -Directory | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).FullName
    }
    $release=(Resolve-Path -LiteralPath $ReleaseDirectory).Path
    [xml]$project=Get-Content -LiteralPath 'HoldItTogether.csproj' -Raw
    $date=[datetime]::Parse([string]$project.Project.PropertyGroup.HitReleaseDate)
    $version='v'+$date.ToString('ddMM')
    $application=Join-Path $release ('HIT-'+$version+'-win-x64-self-contained')
    if (!(Test-Path -LiteralPath (Join-Path $application 'coreclr.dll'))) { throw 'Build the self-contained release first.' }
    if (!$Compiler) {
        $candidates=@((Join-Path $workspace 'artifacts/tools/innosetup/ISCC.exe'),"${env:ProgramFiles(x86)}/Inno Setup 6/ISCC.exe","$env:ProgramFiles/Inno Setup 7/ISCC.exe")
        $Compiler=$candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
    if (!$Compiler) { throw 'Install Inno Setup 6.7 or later, or pass -Compiler with the ISCC.exe path. See Docs/INSTALLER.md.' }
    $bootstrapper=Join-Path $workspace 'artifacts/tools/MicrosoftEdgeWebview2Setup.exe'
    if (!(Test-Path -LiteralPath $bootstrapper)) {
        [IO.Directory]::CreateDirectory((Split-Path -Parent $bootstrapper)) | Out-Null
        Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $bootstrapper -UseBasicParsing
    }
    $signature=Get-AuthenticodeSignature -LiteralPath $bootstrapper
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { throw 'The WebView2 bootstrapper must have a valid Microsoft signature.' }
    $payload=Join-Path $workspace ('artifacts/installer-staging/'+[Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($payload) | Out-Null
    Copy-Item -LiteralPath $application -Destination (Join-Path $payload 'App') -Recurse
    $developer=Join-Path $payload 'Developer'
    [IO.Directory]::CreateDirectory($developer) | Out-Null
    Copy-Item -LiteralPath (Join-Path $release 'sdk') -Destination (Join-Path $developer 'sdk') -Recurse
    [IO.Directory]::CreateDirectory((Join-Path $developer 'packages')) | Out-Null
    $example=Get-Content -LiteralPath 'Modules/HIT.Example/manifest.json' -Raw | ConvertFrom-Json
    $chess=Get-Content -LiteralPath 'Modules/HIT.Chess/manifest.json' -Raw | ConvertFrom-Json
    Copy-Item -LiteralPath (Join-Path $release ('modules/'+$example.id+'-'+$example.version+'.hitmenu')) -Destination (Join-Path $developer 'packages')
    Copy-Item -LiteralPath (Join-Path $release ('modules/'+$chess.id+'-'+$chess.version+'.hitmenu')) -Destination (Join-Path $payload 'Chess.hitmenu')
    Copy-Item -LiteralPath $bootstrapper -Destination (Join-Path $payload 'WebView2Setup.exe')
    function CopySourceDirectory([string]$source,[string]$destination) {
        [IO.Directory]::CreateDirectory($destination) | Out-Null
        foreach($item in Get-ChildItem -LiteralPath $source -Force) {
            if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){continue}
            if($item.PSIsContainer){
                if($item.Name -notin @('bin','obj','artifacts','.git','.vs','.idea')){CopySourceDirectory $item.FullName (Join-Path $destination $item.Name)}
            }elseif($item.Extension -notin @('.user','.suo','.log')){Copy-Item -LiteralPath $item.FullName -Destination $destination}
        }
    }
    $sourceRoot=Join-Path $developer 'Source'
    [IO.Directory]::CreateDirectory($sourceRoot) | Out-Null
    foreach($directory in @('Components','Services','Modules','Tools','Tests','Resources','wwwroot','Docs','Installer')) {
        CopySourceDirectory (Join-Path $workspace $directory) (Join-Path $sourceRoot $directory)
    }
    foreach($file in @('Program.cs','HoldItTogether.csproj','HoldItTogether.slnx','global.json','Directory.Build.props','Directory.Build.targets','Directory.Packages.props','.editorconfig','.gitignore','.gitattributes','LICENSE','LICENSE.md','NuGet.Config')) {
        if(Test-Path -LiteralPath $file){Copy-Item -LiteralPath $file -Destination $sourceRoot}
    }
    [IO.File]::WriteAllText((Join-Path $developer 'README.txt'),"HIT developer kit`r`n`r`nSource contains the host, SDK, example module, optional Chess source, tests, docs and build tools.`r`nsdk contains the HIT.Sdk NuGet package. packages contains the Notes example; install it manually through Extensions.`r`nChess is installed only if you select it in Setup. Your source copy is independent of installed module state.`r`nTo build, install a .NET SDK supporting net8.0 and .slnx, then run Tools/Build-Release.ps1 from Source.`r`nNode and Google Chrome are needed for browser tests and SVG-to-ICO regeneration. Inno Setup is needed for installer builds.`r`n")
    $output=Join-Path $release 'installer'
    if($TestBuild){$output=Join-Path $workspace 'obj/installer-tests/compiler'}
    [IO.Directory]::CreateDirectory($output) | Out-Null
    $arguments=@(('/DPayloadDir='+$payload),('/DOutputFolder='+$output),('/DHitVersion='+$version),('/DNumericVersion='+$date.ToString('yyyy.MM.dd')))
    if($TestBuild){$arguments+='/DTestBuild=1'}
    & $Compiler /Qp @arguments (Join-Path $workspace 'Installer/HoldItTogether.iss')
    if($LASTEXITCODE -ne 0){throw 'Installer compilation failed.'}
    Write-Output ('Installer ready: '+(Join-Path $output ('HIT-'+$version+'-win-x64-Setup.exe')))
}
finally {Pop-Location}
