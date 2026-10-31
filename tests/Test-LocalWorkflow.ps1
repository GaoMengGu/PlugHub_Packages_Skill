param([string]$RepositoryRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$Validator = Join-Path $RepositoryRoot 'src/PlugHub.PackagesSkill.StaticValidation/bin/Release/net8.0/PlugHub.PackagesSkill.StaticValidation.dll'
if (-not (Test-Path -LiteralPath $Validator)) { throw 'Build the repository validator in Release before running this check.' }

$Baseline = & dotnet $Validator $RepositoryRoot 2>&1
if ($LASTEXITCODE -ne 0) { throw "Baseline validation failed: $Baseline" }
$TemporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('plughub-workflow-' + [guid]::NewGuid())
$Count = 0
try {
    New-Item -ItemType Directory -Path $TemporaryRoot | Out-Null
    Get-ChildItem -LiteralPath $RepositoryRoot -File -Recurse -Force |
        Where-Object { [IO.Path]::GetRelativePath($RepositoryRoot, $_.FullName) -notmatch '(^|[\\/])(\.git|bin|obj)([\\/]|$)' } |
        ForEach-Object {
            $Copy = Join-Path $TemporaryRoot ([IO.Path]::GetRelativePath($RepositoryRoot, $_.FullName))
            New-Item -ItemType Directory -Path (Split-Path $Copy -Parent) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $Copy
        }
    $CopiedBaseline = & dotnet $Validator $TemporaryRoot 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Copied baseline validation failed: $CopiedBaseline" }

    foreach ($Language in @('zh', 'en')) {
        $Skill = if ($Language -eq 'zh') { 'plughub-package-authoring' } else { 'plughub-package-authoring-en' }
        $Markers = if ($Language -eq 'zh') {
            @('安装状态', '未安装时从核实的官方 GitHub 获取执行时最新版本', '-RevitApiDir', '-UseRevitApiNuGet', '完整包')
        } else {
            @('installed version', 'latest version at execution time from verified official GitHub', '-RevitApiDir', '-UseRevitApiNuGet', 'complete package')
        }
        foreach ($Relative in @('SKILL.md', 'references/authoring-playbook.md')) {
            $File = Join-Path $TemporaryRoot "$Skill/$Relative"
            $Original = [IO.File]::ReadAllText($File)
            foreach ($Marker in $Markers) {
                # Remove entire requirement lines, not just an isolated word.
                $Mutated = ($Original -split "`n" | Where-Object { -not $_.Contains($Marker) }) -join "`n"
                if ($Mutated -eq $Original) { throw "Mutation did not change $File for $Marker" }
                try {
                    [IO.File]::WriteAllText($File, $Mutated)
                    $Output = & dotnet $Validator $TemporaryRoot 2>&1
                    if ($LASTEXITCODE -eq 0 -or "$Output" -notmatch [regex]::Escape("missing local delivery requirement: $Marker")) {
                        throw "Expected local workflow rejection for $Skill/$Relative ($Marker): $Output"
                    }
                    $Count++
                } finally { [IO.File]::WriteAllText($File, $Original) }
            }
        }
        foreach ($Case in @(
            @{ File = 'SKILL.md'; Marker = 'Create a flat, solid glyph icon'; Error = 'icon prompt wording' },
            @{ File = 'tools/PlugHub.PackageValidator/Program.cs'; Marker = 'DisallowedModuleKeys'; Error = 'must reject module runtime state fields' }
        )) {
            $File = Join-Path $TemporaryRoot "$Skill/$($Case.File)"
            $Original = [IO.File]::ReadAllText($File)
            if (-not $Original.Contains($Case.Marker)) { throw "Missing regression marker: $($Case.Marker)" }
            try {
                [IO.File]::WriteAllText($File, $Original.Replace($Case.Marker, 'removed-by-regression-check'))
                $Output = & dotnet $Validator $TemporaryRoot 2>&1
                if ($LASTEXITCODE -eq 0 -or "$Output" -notmatch [regex]::Escape($Case.Error)) {
                    throw "Expected retained contract rejection for $Skill/$($Case.File): $Output"
                }
                $Count++
            } finally { [IO.File]::WriteAllText($File, $Original) }
        }
    }
    Write-Output "Local workflow regression checks passed: $Count mutations rejected; baseline and temporary copy passed."
} finally {
    if (Test-Path -LiteralPath $TemporaryRoot) { Remove-Item -LiteralPath $TemporaryRoot -Recurse -Force }
}
