param([string]$RepositoryRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$TemporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('plughub-startup-' + [guid]::NewGuid())
$Count = 0
try {
    New-Item -ItemType Directory -Path (Join-Path $TemporaryRoot 'dist') -Force | Out-Null
    # These fixtures check JSON and file paths; they do not simulate Revit or load DLLs.
    [IO.File]::WriteAllText((Join-Path $TemporaryRoot 'dist/Example.dll'), 'payload fixture')
    $Cases = @(
        @{ Name = 'omitted startup'; Pass = $true; Edit = {} },
        @{ Name = 'disabled startup'; Pass = $true; Edit = { $Module.autoStart = $false } },
        @{ Name = 'disabled with valid type'; Pass = $true; Edit = { $Module.autoStart = $false; $Module.startupType = 'Example.Startup' } },
        @{ Name = 'startup with command'; Pass = $true; Edit = { $Module.autoStart = $true; $Module.startupType = 'Example.Startup' } },
        @{ Name = 'startup without buttons'; Pass = $true; Edit = { $Module.autoStart = $true; $Module.startupType = 'Example.Startup'; $Module.features = @() } },
        @{ Name = 'empty inert module'; Pass = $false; Error = '.features'; Edit = { $Module.features = @() } },
        @{ Name = 'disabled empty module'; Pass = $false; Error = '.features'; Edit = { $Module.autoStart = $false; $Module.startupType = 'Example.Startup'; $Module.features = @() } },
        @{ Name = 'string autoStart'; Pass = $false; Error = '.autoStart'; Edit = { $Module.autoStart = 'true' } },
        @{ Name = 'number autoStart'; Pass = $false; Error = '.autoStart'; Edit = { $Module.autoStart = 1 } },
        @{ Name = 'null autoStart'; Pass = $false; Error = '.autoStart'; Edit = { $Module.autoStart = $null } },
        @{ Name = 'object autoStart'; Pass = $false; Error = '.autoStart'; Edit = { $Module.autoStart = @{ value = $true } } },
        @{ Name = 'missing startupType'; Pass = $false; Error = '.startupType'; Edit = { $Module.autoStart = $true } },
        @{ Name = 'legacy type is not startup'; Pass = $false; Error = '.startupType'; Edit = { $Module.autoStart = $true; $Module.type = 'Example.Startup' } },
        @{ Name = 'empty startupType'; Pass = $false; Error = '.startupType'; Edit = { $Module.autoStart = $true; $Module.startupType = '' } },
        @{ Name = 'short startupType'; Pass = $false; Error = '.startupType'; Edit = { $Module.autoStart = $true; $Module.startupType = 'Startup' } },
        @{ Name = 'whitespace startupType'; Pass = $false; Error = '.startupType'; Edit = { $Module.autoStart = $true; $Module.startupType = 'Example. Startup' } },
        @{ Name = 'null startupType'; Pass = $false; Error = '.startupType'; Edit = { $Module.startupType = $null } },
        @{ Name = 'numeric startupType while disabled'; Pass = $false; Error = '.startupType'; Edit = { $Module.autoStart = $false; $Module.startupType = 1 } },
        @{ Name = 'missing features'; Pass = $false; Error = '.features'; Edit = { $Module.autoStart = $true; $Module.startupType = 'Example.Startup'; $Module.Remove('features') } },
        @{ Name = 'object features'; Pass = $false; Error = '.features'; Edit = { $Module.autoStart = $true; $Module.startupType = 'Example.Startup'; $Module.features = @{} } },
        @{ Name = 'missing assembly'; Pass = $false; Error = '.assembly'; Edit = { $Module.autoStart = $true; $Module.startupType = 'Example.Startup'; $Module.Remove('assembly') } },
        @{ Name = 'missing DLL'; Pass = $false; Error = '.assembly'; Edit = { $Module.assembly = 'dist/Missing.dll' } },
        @{ Name = 'escaping DLL'; Pass = $false; Error = '.assembly'; Edit = { $Module.assembly = '../Example.dll' } },
        @{ Name = 'startup does not replace commandType'; Pass = $false; Error = '.commandType'; Edit = { $Module.autoStart = $true; $Module.startupType = 'Example.Startup'; $Module.features[0].Remove('commandType') } },
        @{ Name = 'old module enabled'; Pass = $false; Error = '.enabled'; Edit = { $Module.enabled = $false } },
        @{ Name = 'old module visible'; Pass = $false; Error = '.visible'; Edit = { $Module.visible = $false } },
        @{ Name = 'old feature state'; Pass = $false; Error = '.defaultState'; Edit = { $Module.features[0].defaultState = 'Hidden' } },
        @{ Name = 'layout restriction retained'; Pass = $false; Error = '.buttonSize'; Edit = { $Module.features[0].buttonSize = 'large' } }
    )
    foreach ($Skill in @('plughub-package-authoring', 'plughub-package-authoring-en')) {
        $Project = Join-Path $RepositoryRoot "$Skill/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj"
        $Build = & dotnet build $Project -c Release --nologo 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $Build" }
        $Validator = Join-Path (Split-Path $Project -Parent) 'bin/Release/net8.0/PlugHub.PackageValidator.dll'
        foreach ($Case in $Cases) {
            $Module = @{ id = 'example'; version = 'V1.0.0'; assembly = 'dist/Example.dll'; features = @(@{ id = 'example.run'; commandType = 'Example.Command' }) }
            & $Case.Edit
            $Manifest = @{ schemaVersion = '1.1'; revitVersions = @('2020'); frameworkVersionRange = '>=1.3.0'; modules = @($Module) }
            [IO.File]::WriteAllText((Join-Path $TemporaryRoot 'Example.packages.json'), ($Manifest | ConvertTo-Json -Depth 10))
            $Output = & dotnet $Validator $TemporaryRoot --manifest Example.packages.json 2>&1
            if (($LASTEXITCODE -eq 0) -ne $Case.Pass -or (-not $Case.Pass -and "$Output" -notmatch [regex]::Escape($Case.Error))) {
                throw "$Skill / $($Case.Name): unexpected result: $Output"
            }
            $Count++
        }
    }
    Write-Output "Package startup regression passed: $Count cases across both validators."
} finally {
    if (Test-Path -LiteralPath $TemporaryRoot) { Remove-Item -LiteralPath $TemporaryRoot -Recurse -Force }
}
