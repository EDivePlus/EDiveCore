# Checks uncommitted .cs files of a Unity project (Windows, PowerShell 5.1). Run before committing or after bigger edits.
#  - Rider inspections through Rider's bundled quality-check (needs Rider running with the project); files are left as they are
#  - Unity compile state from Editor.log: compile errors, or edits Unity hasn't compiled yet
# Usage: check-changes.ps1 [-Path <dir inside the project>] [-SinceMinutes <n>]
param([string]$Path = (Get-Location).Path, [int]$SinceMinutes = 0)

$ErrorActionPreference = 'SilentlyContinue'
$root = $Path
while ($root -and -not (Test-Path (Join-Path $root 'ProjectSettings'))) { $root = Split-Path $root -Parent }
if (-not $root) { Write-Host 'Not inside a Unity project.'; exit 1 }
$since = if ($SinceMinutes -gt 0) { (Get-Date).AddMinutes(-$SinceMinutes) } else { [DateTime]::MinValue }

# Third-party code is never reformatted or reported
$thirdParty = '(^|/)(LicensedPlugins|Plugins|ExternalAssets|External|Samples|ExternalUtils/[^/]+/External)/'
$repos = @($root) + @(git -C $root submodule status 2>$null | ForEach-Object { Join-Path $root (($_.Trim() -split '\s+')[1]) })
$changed = foreach ($repo in $repos) {
    git -C $repo status --porcelain --untracked-files=all 2>$null | ForEach-Object {
        $path = $_.Substring(3).Trim('"')
        $full = Join-Path $repo $path
        if ($path -like '*.cs' -and ($full -replace '\\', '/') -notmatch $thirdParty) {
            if ((Test-Path $full) -and (Get-Item $full).LastWriteTime -gt $since) { $full }
        }
    }
}
$changed = @($changed | Select-Object -Unique)
$writeTimes = @{}
foreach ($file in $changed) { $writeTimes[$file] = (Get-Item $file).LastWriteTime }
if ($changed.Count -eq 0) { Write-Host 'No changed .cs files.'; exit 0 }

$findings = New-Object System.Collections.Generic.List[string]

# Rider: reuse the bundled quality-check (reformat + inspections) with a synthetic edit event per file
$qc = Get-ChildItem (Join-Path $env:LOCALAPPDATA 'JetBrains\Rider*\aia\bundledHooks\quality-check.ps1') | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($qc -and (Get-Process rider64)) {
    foreach ($file in ($changed | Select-Object -First 15)) {
        # The Rider check always reformats the whole file; keep the file as it was and use only the inspections
        $original = [IO.File]::ReadAllBytes($file)
        $fake = @{ tool_name = 'Edit'; tool_input = @{ file_path = $file }; cwd = $root } | ConvertTo-Json -Compress
        $out = $fake | powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $qc.FullName 2>$null | Out-String
        $after = [IO.File]::ReadAllBytes($file)
        if ($after.Length -ne $original.Length -or [Convert]::ToBase64String($after) -ne [Convert]::ToBase64String($original)) {
            [IO.File]::WriteAllBytes($file, $original)
            (Get-Item $file).LastWriteTime = $writeTimes[$file]
        }
        $context = try { ($out | ConvertFrom-Json).hookSpecificOutput.additionalContext } catch { $out }
        if (-not $context -or $context -match 'check passed|quality-check unavailable') { continue }

        # Only warnings and errors on lines changed against HEAD (all lines for new files)
        $repo = $repos | Where-Object { $file.StartsWith($_ + '\') } | Sort-Object Length -Descending | Select-Object -First 1
        $relative = $file.Substring($repo.Length + 1)
        $lines = New-Object 'System.Collections.Generic.HashSet[int]'
        $tracked = git -C $repo ls-files -- $relative
        if ($tracked) {
            git -C $repo diff HEAD -U0 -- $relative | ForEach-Object {
                if ($_ -match '^@@ -\S+ \+(\d+)(?:,(\d+))? @@') {
                    $start = [int]$Matches[1]; $count = if ($Matches[2]) { [int]$Matches[2] } else { 1 }
                    for ($l = $start; $l -lt $start + $count; $l++) { [void]$lines.Add($l) }
                }
            }
        }
        $problems = $context -split "`n" | Where-Object {
            $_ -match '^- .+?:(\d+):\d+ \[(WARNING|ERROR)\]' -and (-not $tracked -or $lines.Contains([int]$Matches[1]))
        }
        if ($problems) {
            $findings.Add("Rider, $($file.Substring($root.Length + 1)):`n" + ($problems -join "`n"))
        }
    }
}

# Unity: errors after the last successful reload, or sources newer than the compiled assemblies
$log = Join-Path $env:LOCALAPPDATA 'Unity\Editor\Editor.log'
if ((Get-Process Unity) -and (Test-Path $log)) {
    $lines = Get-Content $log -Tail 20000
    $lastReload = -1; $errors = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -like '*Begin MonoManager ReloadAssembly*') { $lastReload = $i; $errors.Clear() }
        elseif ($lines[$i] -match 'error CS\d+') { $errors.Add($lines[$i].Trim()) }
    }
    if ($errors.Count -gt 0) {
        $findings.Add("Unity compile errors:`n" + (($errors | Select-Object -Unique | Select-Object -First 10) -join "`n"))
    }
    else {
        $compiled = Get-ChildItem (Join-Path $root 'Library\ScriptAssemblies\*.dll') | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        $newer = @($changed | Where-Object { $compiled -and $writeTimes[$_] -gt $compiled.LastWriteTime })
        if ($newer.Count -gt 0) {
            $findings.Add('Unity has not compiled these edits yet, refresh and check the console: ' + (($newer | ForEach-Object { Split-Path $_ -Leaf }) -join ', '))
        }
    }
}

if ($findings.Count -eq 0) { Write-Host "OK: $($changed.Count) changed .cs files, no warnings on changed lines, Unity compiled."; exit 0 }
Write-Host ($findings -join "`n`n")
exit 2
