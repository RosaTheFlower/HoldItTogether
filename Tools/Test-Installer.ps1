param([Parameter(Mandatory=$true)][string]$ReleaseDirectory,[string]$Compiler)
$ErrorActionPreference='Stop'
$workspace=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location -LiteralPath $workspace
try {
    & (Join-Path $PSScriptRoot 'Build-Installer.ps1') -ReleaseDirectory $ReleaseDirectory -Compiler $Compiler -TestBuild
    [xml]$project=Get-Content -LiteralPath 'HoldItTogether.csproj' -Raw
    $version='v'+([datetime]::Parse([string]$project.Project.PropertyGroup.HitReleaseDate)).ToString('ddMM')
    $setup=Join-Path $workspace ('obj/installer-tests/compiler/HIT-'+$version+'-win-x64-Setup.exe')
    foreach($profile in @('release','dev')) {
        foreach($chess in @(0,1)) {
            $destination=Join-Path $workspace ('obj/installer-tests/'+$profile+'-chess'+$chess+'-'+[Guid]::NewGuid().ToString('N'))
            $arguments=@('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/NOICONS',('/PROFILE='+$profile),('/CHESS='+$chess),('/DIR="'+$destination+'"'))
            $desktop=($profile -eq 'dev' -and $chess -eq 1)
            if($desktop){$arguments+='/TASKS=desktopicon'}else{$arguments+='/TASKS='}
            $process=Start-Process -FilePath $setup -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
            if($process.ExitCode -ne 0){throw "Installer failed: $profile, Chess $chess, exit $($process.ExitCode)"}
            if(!(Test-Path -LiteralPath (Join-Path $destination 'HoldItTogether.exe'))){throw 'Missing application.'}
            $shortcut=Join-Path $destination 'TestDesktop/HIT.lnk'
            if((Test-Path -LiteralPath $shortcut) -ne $desktop){throw 'The HIT desktop shortcut did not match the selected option.'}
            if($desktop){
                $shell=New-Object -ComObject WScript.Shell
                $link=$shell.CreateShortcut($shortcut)
                if($link.TargetPath -ne (Join-Path $destination 'HoldItTogether.exe')){throw 'The HIT shortcut points to the wrong executable.'}
                [void][Runtime.InteropServices.Marshal]::ReleaseComObject($link)
                [void][Runtime.InteropServices.Marshal]::ReleaseComObject($shell)
            }
            if((Test-Path -LiteralPath (Join-Path $destination 'Developer')) -ne ($profile -eq 'dev')){throw 'Developer kit did not match the selected profile.'}
            if(Test-Path -LiteralPath (Join-Path $destination 'Docs')){throw 'Host documentation should be inside the Dev kit only.'}
            if($profile -eq 'dev') {
                foreach($relative in @('Developer/Source/Program.cs','Developer/Source/HoldItTogether.slnx','Developer/Source/Modules/HIT.Example/.template.config/template.json','Developer/Source/Modules/HIT.Sdk/HIT.Sdk.csproj','Developer/Source/Docs/INSTALLER.md')) {
                    if(!(Test-Path -LiteralPath (Join-Path $destination $relative))){throw "Missing developer file: $relative"}
                }
                if(!(Get-ChildItem -LiteralPath (Join-Path $destination 'Developer/sdk') -Filter '*.nupkg')){throw 'Missing SDK package.'}
                if(!(Get-ChildItem -LiteralPath (Join-Path $destination 'Developer/packages') -Filter '*.hitmenu')){throw 'Missing example package.'}
                if(@(Get-ChildItem -LiteralPath (Join-Path $destination 'Developer/Source') -Directory -Recurse -Force | Where-Object Name -in @('bin','obj','artifacts','.git','.vs')).Count){throw 'Generated or private build directories leaked into the developer kit.'}
                if($chess -eq 0) {
                    & dotnet build (Join-Path $destination 'Developer/Source/HoldItTogether.slnx') -c Release --nologo -p:OutputPath=bin/InstallerSourceReview/
                    if($LASTEXITCODE -ne 0){throw 'The installed developer source kit could not build.'}
                }
            }
            $registry=Join-Path $destination 'TestModules/registry.json'
            if($chess -eq 1) {
                $state=Get-Content -LiteralPath $registry -Raw | ConvertFrom-Json
                if(@($state.packages).Count -ne 1 -or $state.packages[0].manifest.id -ne 'dev.hit.chess'){throw 'Optional Chess was not registered.'}
                $score=Join-Path $destination 'TestModules/State/dev.hit.chess/preserved-test.txt'
                [IO.Directory]::CreateDirectory((Split-Path -Parent $score)) | Out-Null
                [IO.File]::WriteAllText($score,'preserve game state')
                # Reinstall uses the same package without losing extension data.
                $again=Start-Process -FilePath $setup -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
                if($again.ExitCode -ne 0 -or !(Test-Path -LiteralPath $score)){throw 'Reinstallation was not idempotent.'}
            }elseif(Test-Path -LiteralPath (Join-Path $destination 'Optional')){throw 'Unchecked Chess was copied.'}
            $uninstall=Start-Process -FilePath (Join-Path $destination 'unins000.exe') -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -PassThru -Wait
            if($uninstall.ExitCode -ne 0 -or (Test-Path -LiteralPath (Join-Path $destination 'HoldItTogether.exe'))){throw 'Silent uninstall failed.'}
            if($chess -eq 1 -and !(Test-Path -LiteralPath $score)){throw 'Uninstall erased extension state.'}
            Write-Output "PASS: $profile, Chess $chess, file selection, reinstall and uninstall."
        }
    }
}
finally{Pop-Location}
