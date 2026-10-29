param([switch]$RunTests)
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location -LiteralPath $workspace
try {
    function InvokeDotNet([string[]]$Arguments) {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) { throw ('dotnet failed: ' + ($Arguments -join ' ')) }
    }
    [xml]$project = Get-Content -LiteralPath 'HoldItTogether.csproj' -Raw
    $date = [datetime]::Parse([string]$project.Project.PropertyGroup.HitReleaseDate)
    $version = [string]$project.Project.PropertyGroup.HitReleaseGeneration + 'v' + $date.ToString('ddMM')
    $releaseRoot = Join-Path $workspace ('artifacts/releases/' + $version)
    if (Test-Path -LiteralPath $releaseRoot) { $releaseRoot += '-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') }
    [IO.Directory]::CreateDirectory($releaseRoot) | Out-Null
    $modules = Join-Path $releaseRoot 'modules'
    [IO.Directory]::CreateDirectory($modules) | Out-Null

    # Keep developer output and running applications untouched.
    InvokeDotNet @('build','HoldItTogether.slnx','-c','Release','--nologo','-p:OutputPath=bin/DistributionBuild/','-p:CopyRetryCount=0')
    InvokeDotNet @('pack','Modules/HIT.Sdk/HIT.Sdk.csproj','-c','Release','-o',(Join-Path $releaseRoot 'sdk'),'--nologo','-p:OutputPath=bin/DistributionBuild/')
    foreach ($module in @('HIT.Chess','HIT.Example')) {
        & (Join-Path $PSScriptRoot 'Pack-HitMenu.ps1') -Project "Modules/$module/$module.csproj" -OutputDirectory 'artifacts/packages'
        $manifest = Get-Content -LiteralPath "Modules/$module/manifest.json" -Raw | ConvertFrom-Json
        Copy-Item -LiteralPath (Join-Path $workspace ('artifacts/packages/' + $manifest.id + '-' + $manifest.version + '.hitmenu')) -Destination $modules
    }
    if ($RunTests) {
        InvokeDotNet @('Modules/HIT.Example.Tests/bin/DistributionBuild/HIT.Example.Tests.dll')
        InvokeDotNet @('Modules/HIT.Chess.Tests/bin/DistributionBuild/HIT.Chess.Tests.dll')
    }

    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class HitReleaseIconCheck {
    [DllImport("shell32.dll",EntryPoint="ExtractIconExW",CharSet=CharSet.Unicode)]
    public static extern uint Count(string file,int index,IntPtr large,IntPtr small,uint count);
}
'@
    foreach ($mode in @('framework-dependent','self-contained')) {
        $selfContained = ($mode -eq 'self-contained').ToString().ToLowerInvariant()
        $output = Join-Path $releaseRoot ('HIT-' + $version + '-win-x64-' + $mode)
        InvokeDotNet @('publish','HoldItTogether.csproj','-c','Release','-r','win-x64','--self-contained',$selfContained,'-o',$output,'--nologo',
            ('-p:OutputPath=bin/DistributionPublish/' + $mode + '/'),'-p:CopyRetryCount=0','-p:UseAppHost=true','-p:PublishSingleFile=false',
            '-p:PublishTrimmed=false','-p:PublishReadyToRun=false','-p:CopyOutputSymbolsToPublishDirectory=false','-p:DebugType=None')
        foreach ($file in @('HoldItTogether.exe','HoldItTogether.dll','HoldItTogether.runtimeconfig.json','HIT.Sdk.dll','Resources/hit.ico','wwwroot/icons/hit.svg','wwwroot/js/hit-app-icon.js','Docs/README.md')) {
            if (!(Test-Path -LiteralPath (Join-Path $output $file))) { throw "Incomplete publication: $mode / $file" }
        }
        if ([HitReleaseIconCheck]::Count((Join-Path $output 'HoldItTogether.exe'),-1,[IntPtr]::Zero,[IntPtr]::Zero,0) -lt 1) { throw 'The application icon was not embedded in the executable.' }
        $runtimeFiles = @('coreclr.dll','hostfxr.dll','hostpolicy.dll','System.Private.CoreLib.dll')
        foreach ($file in $runtimeFiles) {
            $exists = Test-Path -LiteralPath (Join-Path $output $file)
            if ($exists -ne ($mode -eq 'self-contained')) { throw "Unexpected runtime packaging: $mode / $file" }
        }
        $requirements = if ($mode -eq 'self-contained') { 'The .NET runtime is included. No separate .NET installation is required.' } else { 'Install the Windows x64 .NET 8 runtime and ASP.NET Core 8 runtime before running HIT.' }
        [IO.File]::WriteAllText((Join-Path $output 'README.txt'),"HIT $version - Windows x64 - $mode`r`n`r`n$requirements`r`nMicrosoft Edge WebView2 Runtime is required for both distributions.`r`nExtract the entire ZIP, keep these files together and run HoldItTogether.exe.`r`nChess and Notes are optional .hitmenu modules, distributed separately in the modules folder.`r`n")
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::CreateFromDirectory($output,$output+'.zip',[IO.Compression.CompressionLevel]::Optimal,$true)
    }
    Write-Output "Release ready: $releaseRoot"
    Write-Output 'Both executables include hit.ico. Optional modules and the SDK remain separate.'
}
finally { Pop-Location }
