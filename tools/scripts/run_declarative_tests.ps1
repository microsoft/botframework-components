Param(
	[string] $BuildConfiguration
)

$projectRoot = Join-Path $PSScriptRoot ".." ".."
$testsRoot = Join-Path $projectRoot "skills" "declarative" "tests"
Write-Host $testsRoot
$configFiles = Get-ChildItem -Path $testsRoot -Recurse -Include "config.json"
foreach ($configFile in $configFiles)
{
    $config = Get-Content -Encoding utf8 -Raw -Path $configFile | ConvertFrom-Json
    if ($config.'$kind' -eq 'DeclarativeTestConfiguration')
    {
        $test = $configFile.DirectoryName
        $ut = Join-Path $test $config.ut "DeclarativeUT.csproj"
        $bot = Join-Path $test $config.botFolder
        $result = Join-Path $test "test-result.xml"
        dotnet run --project "$ut" --configuration $BuildConfiguration -- --bot "$bot" --test "$test" --debug true --outputResult "$result"
    }
}