Param(
	[string] $BuildConfiguration
)

$projectRoot = Join-Path $PSScriptRoot ".." ".."
$testsRoot = Join-Path $projectRoot "skills" "declarative" "tests"
$configFiles = Get-ChildItem -Path $testsRoot -Recurse -Include "config.json"
foreach ($configFile in $configFiles)
{
    $config = Get-Content -Encoding utf8 -Raw -Path $configFile | ConvertFrom-Json
    if ($config.'$kind' -eq 'DeclarativeTestConfiguration')
    {
        $test = $configFile.DirectoryName
        $ut = Join-Path $test $config.ut "DeclarativeUT.csproj"
        $bot = Join-Path $test $config.botFolder
        $result = Join-Path $test "TestResults.xml"
        dotnet run --project "$ut" --configuration $BuildConfiguration -- --bot "$bot" --test "$test" --debug true --outputResult "$result"
        # TODO: simplify parameters; they are less informative
        #$botFolder = 'TestRunParameters.Parameter(name=\\\"botFolder\\\",value=\\\"' + $bot + '\\\")'
        #$testFolder = 'TestRunParameters.Parameter(name=\\\"testFolder\\\",value=\\\"' + $test + '\\\")'
        #& dotnet test $ut --results-directory $test --logger:xunit '--%' '--' $botFolder $testFolder
    }
}