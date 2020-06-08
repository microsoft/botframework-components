#Requires -Version 6

Param(
	[string] $src
)

if (-not $src) {
    $src = Read-Host "? Path to skill sample"
}

$target = Join-Path "Deployment" "Scripts"
$src = Join-Path $src $target "*"
$projectRoot = Join-Path $PSScriptRoot ".." ".."

function GetSrc
{
    return Join-Path $projectRoot @args $target "*"
}

function AddPath
{
    param($dsts)
    $dsts.Add($(Join-Path $projectRoot @args $target)) > $null
}

function Synchronize
{
    param($src, $dsts)

    Write-Host "Copy from $src"
    foreach ($dst in $dsts)
    {
        Write-Host "Copy to $dst"
        Copy-Item "$src" -Destination "$dst"
    }    
}

# synchronize skills

$dsts = [System.Collections.ArrayList]@()
$skills = @("calendarskill", "emailskill", "pointofinterestskill", "todoskill")
foreach ($skill in $skills)
{
    AddPath $dsts "skills" "csharp" $skill
}
$expSkills = @("automotiveskill", "bingsearchskill", "eventskill", "hospitalityskill", "itsmskill", "musicskill", "newsskill", "phoneskill", "restaurantbookingskill", "weatherskill")
foreach ($skill in $expSkills)
{
    AddPath $dsts "skills" "csharp" "experimental" $skill
}
Synchronize $src $dsts
