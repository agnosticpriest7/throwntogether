[CmdletBinding()]
param(
    [ValidateSet('Deploy','Build','Test')][string]$Mode = 'Deploy',
    [string]$UnityPath,
    [int]$TimeoutMinutes = 90
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$repo = 'agnosticpriest7/throwntogether'
$url = 'https://agnosticpriest7.github.io/throwntogether/'
function Git([string[]]$Arguments) {
    $value = & git.exe -C $root @Arguments
    if ($LASTEXITCODE -ne 0) { throw "git failed: $Arguments" }
    return $value
}
function Assert-Clean {
    if (@(Git @('status','--porcelain=v1','--untracked-files=all')).Count) {
        throw 'Commit or stash source changes before running. Only committed source is tested/built.'
    }
}
function Remove-OwnedDirectory([string]$Path, [string]$Parent) {
    $full = [IO.Path]::GetFullPath($Path)
    $prefix = [IO.Path]::GetFullPath($Parent).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (!$full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe cleanup path: $full" }
    if (Test-Path -LiteralPath $full) {
        if ((Get-Item -LiteralPath $full).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing linked directory.' }
        Remove-Item -LiteralPath $full -Recurse -Force
    }
}
if ((Git @('rev-parse','--show-toplevel')).Replace('\','/') -ne $root.Replace('\','/')) { throw 'Wrong repository root.' }
if ((Git @('branch','--show-current')) -ne 'main') { throw 'Run from main.' }
if ((Git @('remote','get-url','origin')) -notmatch '^https://github\.com/agnosticpriest7/throwntogether(?:\.git)?$') { throw 'Unexpected origin.' }
Assert-Clean
$sha = Git @('rev-parse','HEAD')
if ($Mode -eq 'Deploy') {
    Git @('fetch','origin') | Out-Host
    if ((Git @('rev-parse','origin/main')) -ne $sha) { throw 'Push main and resolve any divergence before deployment.' }
}
$version = (Select-String -Path "$root/ProjectSettings/ProjectVersion.txt" -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
if (!$UnityPath) { $UnityPath = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe" }
if (!(Test-Path -LiteralPath $UnityPath)) { throw "Unity $version not found; supply -UnityPath." }
$builds = Join-Path $root 'Builds'
New-Item -ItemType Directory -Path $builds -Force | Out-Null
$lock = $null
try {
    $lock = [IO.File]::Open((Join-Path $builds 'pipeline.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    $workspace = Join-Path $builds 'Workspace'
    $logs = Join-Path $builds 'PipelineLogs'
    New-Item -ItemType Directory -Path $workspace,$logs -Force | Out-Null
    if ((Get-Item $workspace).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing linked workspace.' }
    # Replace only our snapshot source. Keep its Library cache for subsequent builds.
    foreach ($folder in @('Assets','Packages','ProjectSettings')) { Remove-OwnedDirectory (Join-Path $workspace $folder) $workspace }
    $archive = Join-Path $builds 'source.tar'
    Git @('archive','--format=tar',"--output=$archive",$sha,'Assets','Packages','ProjectSettings','build-config.json') | Out-Host
    & tar -xf $archive -C $workspace
    if ($LASTEXITCODE -ne 0) { throw 'Source extraction failed.' }
    function Run-Unity([string]$Name, [string[]]$Extra) {
        # Unity 6000.6 can fail rewriting cached response files on the next process.
        # Regenerate only these tiny compiler inputs; retain native/asset build caches.
        Remove-OwnedDirectory (Join-Path $workspace 'Library/Bee/artifacts/rsp') $workspace
        $log = Join-Path $logs "$Name.log"
        $arguments = @('-batchmode','-projectPath',$workspace,'-logFile',$log) + $Extra
        $quoted = $arguments | ForEach-Object { '"' + $_ + '"' }
        Write-Host "Unity $Name running; log: $log"
        $process = Start-Process -FilePath $UnityPath -ArgumentList $quoted -WindowStyle Hidden -PassThru
        $descendants = @{}
        $parents = [Collections.Generic.HashSet[int]]::new()
        [void]$parents.Add($process.Id)
        $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
        while (!$process.WaitForExit(1000)) {
            # Record this batch process's children before intermediaries exit.
            # Never stop helpers belonging to the open canonical Editor.
            $snapshot = @(Get-CimInstance Win32_Process)
            do {
                $added = $false
                foreach ($child in $snapshot) {
                    if ($parents.Contains([int]$child.ParentProcessId) -and !$parents.Contains([int]$child.ProcessId)) {
                        [void]$parents.Add([int]$child.ProcessId)
                        $descendants[[int]$child.ProcessId] = $child
                        $added = $true
                    }
                }
            } while ($added)
            if ((Get-Date) -gt $deadline) { $process.Kill(); $process.WaitForExit(); throw "Unity $Name timed out; see $log" }
        }
        foreach ($child in $descendants.Values) {
            if ($child.Name -notmatch '^(bee_backend|dotnet|Unity\.ILPP\..*|UnityShaderCompiler)\.exe$') { continue }
            $remaining = Get-CimInstance Win32_Process -Filter "ProcessId=$($child.ProcessId)"
            if ($remaining -and $remaining.CreationDate -eq $child.CreationDate) {
                Stop-Process -Id $child.ProcessId -Force -ErrorAction SilentlyContinue
            }
        }
        if ($process.ExitCode -ne 0) { throw "Unity $Name exited $($process.ExitCode); see $log" }
    }
    foreach ($suite in @('EditMode','PlayMode')) {
        $xml = Join-Path $logs "$suite.xml"
        if (Test-Path $xml) { Remove-Item -LiteralPath $xml }
        Run-Unity $suite @('-runTests','-testPlatform',$suite,'-testResults',$xml)
        if (!(Test-Path $xml)) { throw "$suite produced no result file." }
        [xml]$results = Get-Content -LiteralPath $xml -Raw
        $run = $results.'test-run'
        if ($run.result -ne 'Passed' -or [int]$run.total -lt 1 -or [int]$run.failed -ne 0) { throw "$suite failed or discovered no tests; see $xml" }
        Write-Host "$suite passed: $($run.passed) tests."
    }
    Assert-Clean
    if ($Mode -eq 'Test') { return }
    $output = Join-Path $builds 'Web'
    Remove-OwnedDirectory $output $builds
    Run-Unity 'Web' @('-quit','-buildTarget','WebGL','-executeMethod','ThrownTogether.Editor.BuildAutomation.BuildWeb','-buildOutput',$output)
    if (!(Test-Path "$output/index.html") -or !(Get-ChildItem "$output/Build" -Filter '*.wasm') -or !(Get-ChildItem "$output/Build" -Filter '*.data')) { throw 'Incomplete Web output.' }
    if (Get-ChildItem $output -Recurse -File | Where-Object Extension -In '.gz','.br','.unityweb') { throw 'Unexpected compressed output.' }
    if (Get-ChildItem $output -Recurse -File | Where-Object Length -GE 100MB) { throw 'Output exceeds GitHub single-file limit.' }
    Set-Content -LiteralPath "$output/.nojekyll" -Value '' -NoNewline
    @{ sourceCommit=$sha; builtAtUtc=[DateTime]::UtcNow.ToString('o'); scenes=(Get-Content "$root/build-config.json" -Raw | ConvertFrom-Json).scenes } | ConvertTo-Json | Set-Content "$output/build-info.json"
    Write-Host "Web build: $output"
    Assert-Clean
    if ((Git @('rev-parse','HEAD')) -ne $sha) { throw 'Source commit changed during build.' }
    if ($Mode -eq 'Build') { return }
    Git @('fetch','origin') | Out-Host
    if ((Git @('rev-parse','origin/main')) -ne $sha) { throw 'Remote main changed during build.' }
    $legacy = Git @('ls-remote','origin','refs/heads/legacy/web-prototype','refs/tags/web-prototype-final','refs/tags/web-prototype-final^{}')
    # Dedicated temporary repository: canonical main never switches branches.
    $publish = Join-Path $builds ('Publish-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $publish | Out-Null
    function Publish-Git([string[]]$Arguments) {
        & git.exe -C $publish @Arguments | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Deployment git failed: $Arguments. Inspect $publish" }
    }
    Publish-Git @('init','-b','gh-pages')
    Publish-Git @('remote','add','origin',(Git @('remote','get-url','origin')))
    $existing = Git @('ls-remote','--heads','origin','gh-pages')
    if ($existing) {
        Publish-Git @('fetch','origin','gh-pages')
        Publish-Git @('reset','--mixed','FETCH_HEAD')
    }
    Get-ChildItem -LiteralPath $output -Force | Copy-Item -Destination $publish -Recurse
    Publish-Git @('add','-A')
    Publish-Git @('commit','-m',"Deploy Unity Web from $sha")
    Publish-Git @('push','origin','HEAD:refs/heads/gh-pages')
    $pagesConfig = Join-Path $builds 'pages-settings.json'
    @{build_type='legacy';source=@{branch='gh-pages';path='/'}} | ConvertTo-Json | Set-Content $pagesConfig
    & gh api --method PUT "repos/$repo/pages" --input $pagesConfig | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Build published. Human action: GitHub Settings > Pages > Deploy from a branch > gh-pages > / (root) > Save. URL: $url" }
    # The initial switch from Actions to branch publishing may not schedule a build.
    & gh api --method POST "repos/$repo/pages/builds" | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Static files pushed, but Pages build request failed. Check GitHub Pages deployment status before refreshing $url" }
    $after = Git @('ls-remote','origin','refs/heads/legacy/web-prototype','refs/tags/web-prototype-final','refs/tags/web-prototype-final^{}')
    if (($legacy -join "`n") -ne ($after -join "`n")) { throw 'Legacy refs changed during deployment; investigate.' }
    Assert-Clean
    Write-Host "Published gh-pages. Pages may take a few minutes: $url"
} finally {
    if ($null -ne $lock) { $lock.Dispose() }
}
