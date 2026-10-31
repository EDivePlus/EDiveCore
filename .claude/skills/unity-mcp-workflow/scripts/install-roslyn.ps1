# Installs Roslyn for MCP execute_code into a per-machine folder shared by all projects.
# Loaded by EDIVE ExternalRoslynLoader. Picked up on the next domain reload.

$ErrorActionPreference = 'Stop'
$version = '4.12.0'
$target = Join-Path $env:LOCALAPPDATA "UnityRoslyn\$version"

$packages = @(
    @('microsoft.codeanalysis.common', $version, 'Microsoft.CodeAnalysis.dll'),
    @('microsoft.codeanalysis.csharp', $version, 'Microsoft.CodeAnalysis.CSharp.dll'),
    @('system.collections.immutable', '8.0.0', 'System.Collections.Immutable.dll'),
    @('system.reflection.metadata', '8.0.0', 'System.Reflection.Metadata.dll'),
    @('system.runtime.compilerservices.unsafe', '6.0.0', 'System.Runtime.CompilerServices.Unsafe.dll')
)

Add-Type -AssemblyName System.IO.Compression.FileSystem
New-Item -ItemType Directory -Force $target | Out-Null
$temp = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid())
New-Item -ItemType Directory $temp | Out-Null

try {
    foreach ($p in $packages) {
        $id, $ver, $dll = $p
        $nupkg = Join-Path $temp "$id.$ver.nupkg"
        Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/$id/$ver/$id.$ver.nupkg" -OutFile $nupkg
        $zip = [IO.Compression.ZipFile]::OpenRead($nupkg)
        try {
            $entry = $zip.Entries | Where-Object FullName -eq "lib/netstandard2.0/$dll"
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $target $dll), $true)
        } finally { $zip.Dispose() }
        Write-Host "$dll $ver"
    }
} finally { Remove-Item -Recurse -Force $temp }

Write-Host "Installed to $target"
