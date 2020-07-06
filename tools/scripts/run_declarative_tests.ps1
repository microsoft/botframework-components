Param(
	[string] $BuildConfiguration
)

$projectRoot = Join-Path $PSScriptRoot ".." ".."
$testsRoot = Join-Path $projectRoot "skills" "declarative" "tests"
$configFiles = Get-ChildItem -Path $testsRoot -Recurse -Include "config.json"
$exitCode = 0
foreach ($configFile in $configFiles)
{
    $config = Get-Content -Encoding utf8 -Raw -Path $configFile | ConvertFrom-Json
    if ($config.'$kind' -eq 'DeclarativeTestConfiguration')
    {
        # Weird documents.. https://devblogs.microsoft.com/scripting/use-a-powershell-cmdlet-to-work-with-file-attributes/
        $test = $configFile.DirectoryName
        $ut = Join-Path $test $config.ut "DeclarativeUT.csproj"
        $bot = Join-Path $test $config.botFolder
        $result = Join-Path $test "TestResults.xml"

        Write-Host $bot $test $result

        dotnet build "$ut" --configuration $BuildConfiguration --verbosity quiet
        $exe = Join-Path $test $config.ut "bin" $BuildConfiguration "netcoreapp3.1" "DeclarativeUT.exe"
        Write-Host $exe
        $p = Start-Process -FilePath $exe -ArgumentList "--bot", "$bot", "--test", "$test", "--debug", "true", "--outputResult", "$result" -Wait -NoNewWindow -PassThru
        $code = $p.ExitCode

        # TODO: dotnet run doesn't return return code of project?
        #dotnet run --project "$ut" --configuration $BuildConfiguration -- --bot "$bot" --test "$test" --debug true --outputResult "$result"

        # TODO: simplify parameters; they are less informative
        #$botFolder = 'TestRunParameters.Parameter(name=\\\"botFolder\\\",value=\\\"' + $bot + '\\\")'
        #$testFolder = 'TestRunParameters.Parameter(name=\\\"testFolder\\\",value=\\\"' + $test + '\\\")'
        #& dotnet test $ut --results-directory $test --logger:xunit '--%' '--' $botFolder $testFolder

        if ($code -ne 0)
        {
            $exitCode = 1
        }
    }
}

exit $exitCode
