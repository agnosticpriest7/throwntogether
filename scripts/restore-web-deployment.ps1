[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{40}$')][string]$DeploymentCommit,
    [switch]$Publish
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
function Git([string]$Directory,[string[]]$Arguments) {
    $result=& git.exe -C $Directory @Arguments
    if($LASTEXITCODE -ne 0) { throw "Git failed: $Arguments" }
    return $result
}
if((Git $root @('branch','--show-current')) -ne 'main') {throw 'Run from canonical main.'}
if(@(Git $root @('status','--porcelain=v1','--untracked-files=all')).Count) {throw 'Main must be clean.'}
$origin=Git $root @('remote','get-url','origin')
if($origin -notmatch '^https://github\.com/agnosticpriest7/throwntogether(?:\.git)?$') {throw 'Unexpected origin.'}
$head=Git $root @('rev-parse','HEAD')
Git $root @('fetch','origin') | Out-Host
if((Git $root @('rev-parse','origin/main')) -ne $head) {throw 'Main must match freshly fetched origin/main.'}
& git.exe -C $root merge-base --is-ancestor $DeploymentCommit origin/gh-pages
if($LASTEXITCODE -ne 0) {throw 'Select a commit from the existing gh-pages history.'}
$legacy=Git $root @('ls-remote','origin','refs/heads/legacy/web-prototype','refs/tags/web-prototype-final','refs/tags/web-prototype-final^{}')
$tree=Git $root @('ls-tree','-r',$DeploymentCommit)
if($tree | Where-Object {$_ -match '^120000'}) {throw 'Refusing symbolic links in a deployment snapshot.'}
$publishRoot=Join-Path $root ('Builds/Restore-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
$archive=Join-Path $publishRoot 'snapshot.tar'
Git $root @('archive','--format=tar',"--output=$archive",$DeploymentCommit) | Out-Host
$static=Join-Path $publishRoot 'static'
New-Item -ItemType Directory -Path $static | Out-Null
& tar -xf $archive -C $static
if($LASTEXITCODE -ne 0) {throw 'Could not extract deployment snapshot.'}
# Older known-good builds predate manifests; create one from their Git-verified bytes.
$check=if(Test-Path "$static/build-manifest.json") {'--check'} else {'--write'}
& node "$PSScriptRoot/check-web-build.cjs" $check $static
if($LASTEXITCODE -ne 0) {throw 'Snapshot is not a valid deployable Web artifact.'}
$info=Get-Content "$static/build-info.json" -Raw | ConvertFrom-Json
Write-Host "Prepared deployment $DeploymentCommit (source $($info.sourceCommit)) at $static"
if(!$Publish) {Write-Host 'Inspection only. Add -Publish to restore this snapshot as a new gh-pages commit.'; return}
Git $static @('init','-b','gh-pages') | Out-Host
Git $static @('config','core.autocrlf','false') | Out-Host
Git $static @('remote','add','origin',$origin) | Out-Host
Git $static @('fetch','origin','gh-pages') | Out-Host
Git $static @('reset','--mixed','FETCH_HEAD') | Out-Host
Git $static @('add','-A') | Out-Host
Git $static @('commit','-m',"Restore Unity Web deployment $DeploymentCommit (source $($info.sourceCommit))") | Out-Host
Git $static @('push','origin','HEAD:refs/heads/gh-pages') | Out-Host
& gh api --method POST repos/agnosticpriest7/throwntogether/pages/builds
if($LASTEXITCODE -ne 0) {throw 'Snapshot pushed; check GitHub Pages deployment status manually.'}
$after=Git $root @('ls-remote','origin','refs/heads/legacy/web-prototype','refs/tags/web-prototype-final','refs/tags/web-prototype-final^{}')
if(($legacy -join "`n") -ne ($after -join "`n")) {throw 'Legacy refs changed; investigate.'}
if((Git $root @('rev-parse','HEAD')) -ne $head -or @(Git $root @('status','--porcelain=v1','--untracked-files=all')).Count) {throw 'Canonical state changed during publication; investigate.'}
Write-Host 'Restored with a new gh-pages commit. Main/history were not rewritten. https://agnosticpriest7.github.io/throwntogether/'
