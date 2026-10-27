# Safely publish RelayDesk sources to an existing EMPTY GitHub repository.
# Requires Git for Windows and GitHub CLI authenticated to the authorized account.
param(
    [string]$Owner = 'computervirtualservices',
    [string]$Repository = 'RelayDesk-Windows'
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
foreach ($tool in @('git', 'gh')) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "Missing $tool. Install Git for Windows and GitHub CLI first."
    }
}
& gh auth status
if ($LASTEXITCODE -ne 0) { throw 'Authenticate using: gh auth login' }
& gh repo view "$Owner/$Repository" --json name --jq .name
if ($LASTEXITCODE -ne 0) { throw "Create the public repository $Owner/$Repository before using this script." }
if (-not (Test-Path .git)) { & git init -b main; if ($LASTEXITCODE -ne 0) { throw 'git init failed' } }
$remote = "https://github.com/$Owner/$Repository.git"
$existing = (& git remote get-url origin 2>$null)
if ($LASTEXITCODE -eq 0 -and $existing -and $existing -notlike "*$Owner/$Repository*") {
    throw "Refusing to change existing origin remote: $existing"
}
if (-not $existing) { & git remote add origin $remote }
$heads = (& git ls-remote --heads origin main)
if ($LASTEXITCODE -ne 0) { throw 'Cannot check remote branch. Refusing to push.' }
if ($heads) { throw 'Remote main already contains commits. Clone the repository and open a reviewed pull request instead; this bootstrap script never overwrites it.' }
& git add --all
if ($LASTEXITCODE -ne 0) { throw 'git add failed' }
& git status --short
$confirmation = Read-Host "These files will be PUBLIC at $Owner/$Repository. Type PUBLISH PUBLIC to continue"
if ($confirmation -cne 'PUBLISH PUBLIC') { throw 'Cancelled' }
& git commit -m 'Initial RelayDesk Windows source and documentation'
if ($LASTEXITCODE -ne 0) { throw 'Commit failed. Check git user.name and user.email.' }
& git push -u origin main
if ($LASTEXITCODE -ne 0) { throw 'Push failed; inspect GitHub before retrying.' }
Write-Host "Published: https://github.com/$Owner/$Repository"
Write-Host 'To deploy the website, enable GitHub Pages -> GitHub Actions in repository Settings.'
